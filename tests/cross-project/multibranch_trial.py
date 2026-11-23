"""Existing catalog copy, invented branches/quantities only. No network."""
import json,os,sys
from pathlib import Path
from uuid import uuid4
if sys.argv[1]=='--generate':
 rows=[]
 for i in range(501):
  digits='999%09d'%i
  check=(-sum(int(c)*(3 if j%2==0 else 1)for j,c in enumerate(reversed(digits))))%10
  rows.append(dict(trade_en='INVENTED TEST PRODUCT %s'%i,trade_ar='',strength='TEST',form='TEST',barcode=digits+str(check),sell_price=10.0+i))
 Path(sys.argv[2]).write_text(json.dumps(rows),encoding='utf-8');sys.exit(0)
sys.path.insert(0,sys.argv[1])
from database import DB
from availability_sync import Store,KEY_ENV
from availability_bridge import mapping_candidates
root=Path(sys.argv[2]);rows=json.loads(Path(sys.argv[3]).read_text(encoding='utf-8'));setup=json.loads(Path(sys.argv[4]).read_text())
os.environ[KEY_ENV]='synthetic-multibranch-key-not-a-real-grant'
def emit(x):print(json.dumps(x),flush=True)
for n,branch in enumerate(setup['branches']):
 db=DB(path=str(root/('fake-sales-%s.db'%n)))
 # Fresh isolated database only; never open the user's sales database.
 db.conn.execute('DELETE FROM barcodes');db.conn.execute('DELETE FROM drugs');db.conn.commit()
 for i,r in enumerate(rows):
  db.conn.execute('INSERT INTO drugs(id,trade_en,trade_ar,strength,form,sell_price,strips_per_box,tablets_per_strip,active) VALUES(?,?,?,?,?,?,?,?,1)',(i+1,r['trade_en'],r.get('trade_ar')or'',r.get('strength')or'',r.get('form')or'',r['sell_price'],1,1))
  db.conn.execute('INSERT INTO barcodes(barcode,drug_id,pack_level) VALUES(?,?,?)',(r['barcode'],i+1,'box'))
  db.conn.execute('INSERT INTO batches(drug_id,expiry,qty_units) VALUES(?,?,?)',(i+1,'2099-12-31',0 if n==0 else (i%7)+n+1))
 db.conn.commit()
 catalog=[{'id':m,'brandName':r['trade_en'],'strength':r.get('strength')or'','dosageForm':r.get('form')or'','barcodes':[{'barcode':r['barcode']}]}for r,m in zip(rows,setup['medicines'])]
 candidates=mapping_candidates(db,catalog);assert len(candidates)==501 and all(c['status']=='needs_review' for c in candidates)
 assert mapping_candidates(db,[])[0]['status']=='unmatched'
 duplicate=dict(catalog[0],id=str(uuid4()))
 assert mapping_candidates(db,catalog+[duplicate])[0]['status']=='ambiguous'
 # In this isolated trial, local/remote records are identical exported copies.
 # Pack size is a TEST assumption of1, never a reviewed live medicine pack.
 store=Store(str(root/('fake-sidecar-%s.db'%n)))
 store.conn.executemany('INSERT INTO bindings VALUES(?,?,?,1)',[(c['externalProductId'],c['candidateMedicineIds'][0],c['identityFingerprint'])for c in candidates[:500]])
 store.conn.commit();store.configure('https://synthetic.invalid/api/v1/integration/inventory-snapshots',branch,True)
 p=store.stage(db);assert len(p['items'])==500
 def transport(url,key,payload):
  emit({'kind':'request','branchIndex':n,'manifest':store.provision_manifest(),'payload':payload})
  ack=json.loads(sys.stdin.readline());return ack['status'],ack.get('ack',{})
 # Test the limit while a500-row snapshot is waiting for acknowledgement.
 c=candidates[500];store.conn.execute('INSERT INTO bindings VALUES(?,?,?,1)',(c['externalProductId'],c['candidateMedicineIds'][0],c['identityFingerprint']));store.conn.commit()
 pending_limit=store.stage(db)
 assert pending_limit is None and not store.config()['enabled']
 assert not store.send_once(lambda *args:(_ for _ in ()).throw(AssertionError('Must not send while501 bindings exist')))
 emit({'kind':'limit_observation','branchIndex':n,'pending501Blocked':True,'enabled':False})
 store.conn.execute('DELETE FROM bindings WHERE product=?',(c['externalProductId'],));store.conn.commit()
 if not store.config()['enabled']:store.configure('https://synthetic.invalid/api/v1/integration/inventory-snapshots',branch,True)
 assert store.send_once(transport)
 # 501 is a hard stop, never silent truncation or implicit zeroing.
 c=candidates[500];store.conn.execute('INSERT INTO bindings VALUES(?,?,?,1)',(c['externalProductId'],c['candidateMedicineIds'][0],c['identityFingerprint']));store.conn.commit()
 assert store.stage(db) is None and not store.config()['enabled']
 store.close();db.close()
emit({'kind':'done','branches':3,'itemsPerBranch':500,'limit501':'blocked','matching':'501 exact candidates, all required review','packAssumption':'1 TEST unit per pack; not live pack verification'})
