"""Synthetic cross-project trial driver, no external network, no real credentials."""
import json,os,sys,tempfile
from pathlib import Path
from datetime import date,timedelta
sys.path.insert(0,sys.argv[1])
from database import DB
from availability_sync import Store,KEY_ENV
root,branch,medicine=sys.argv[2:5]
os.environ[KEY_ENV]='synthetic-cross-project-key-not-a-real-grant'
db=DB(path=str(Path(root)/'fake-sales.db'))
drug=db.add_drug({'trade_en':'SYNTHETIC trial medicine','form':'Tablet','strength':'10mg','strips_per_box':2,'tablets_per_strip':10,'sell_price':40,'barcode':'4006381333931'})
db.conn.execute('INSERT INTO batches(drug_id,expiry,qty_units) VALUES(?,?,?)',(drug,(date.today()+timedelta(days=60)).isoformat(),45));db.conn.commit()
store=Store(str(Path(root)/'fake-sidecar.db'))
store.review(db,[{'id':medicine,'isActive':True,'barcodes':[{'barcode':'04006381333931'}]}],drug,medicine)
store.configure('https://synthetic.invalid/api/v1/integration/inventory-snapshots',branch,True)
def emit(value):print(json.dumps(value),flush=True)
def transport(endpoint,key,payload):
 emit({'kind':'request','key':key,'manifest':store.provision_manifest(),'payload':payload})
 result=json.loads(sys.stdin.readline())
 return result['status'],result.get('ack',{})
first=store.stage(db)
assert first['items'][0]['availableQuantity']==2
assert not store.send_once(lambda *args:(503,{}))
store.close();store=Store(str(Path(root)/'fake-sidecar.db'))
assert store.stage(db)['snapshotId']==first['snapshotId']
store.conn.execute('UPDATE outbox SET next_attempt=0');store.conn.commit()
assert store.send_once(transport)
# Lost acknowledgement replay is sent directly with the identical payload.
status,ack=transport('',os.environ[KEY_ENV],first);assert status==200 and ack['replay'] is True
# Sell out fake stock. New observation must explicitly clear the public offer.
db.conn.execute('UPDATE batches SET qty_units=0 WHERE drug_id=?',(drug,));db.conn.commit()
second=store.stage(db);assert second['items'][0]['availableQuantity']==0
assert store.send_once(transport)
assert store.pending() is None
emit({'kind':'done','checks':['whole-box quantity2','temporary outage','restart preserves snapshot','durable ack clears queue','idempotent replay','explicit sold-out zero']})
store.close();db.close()
