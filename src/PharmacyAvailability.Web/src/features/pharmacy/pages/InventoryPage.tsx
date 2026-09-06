import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Boxes, History, Search } from 'lucide-react';
import { useAuthStore } from '@/app/store/authStore';
import { useBranchStore } from '@/app/store/branchStore';
import { pharmacyOpsApi } from '../api/pharmacyOpsApi';
import { InventoryItemDto, InventoryTransactionDto } from '@/types/inventory';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { LoadingSpinner } from '@/components/feedback/LoadingSpinner';
import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { formatCurrency, formatDateTime } from '@/lib/formatters';

export function InventoryPage() {
  const { user } = useAuthStore();
  const { currentBranch } = useBranchStore();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');

  // Stock Adjustment Modal
  const [adjustItem, setAdjustItem] = useState<InventoryItemDto | null>(null);
  const [deltaQuantity, setDeltaQuantity] = useState<number>(0);
  const [adjustReason, setAdjustReason] = useState('ManualAdjustment');
  const [adjustNotes, setAdjustNotes] = useState('');

  // Price Update Modal
  const [priceItem, setPriceItem] = useState<InventoryItemDto | null>(null);
  const [newPrice, setNewPrice] = useState<number>(0);

  // Transaction History Modal
  const [historyItem, setHistoryItem] = useState<InventoryItemDto | null>(null);

  const pharmacyId = user?.pharmacyId;
  const branchId = currentBranch?.id;

  const { data: inventory, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['branch-inventory', pharmacyId, branchId],
    queryFn: () =>
      pharmacyId && branchId
        ? pharmacyOpsApi.getBranchInventory(pharmacyId, branchId)
        : Promise.reject('Missing branch'),
    enabled: !!pharmacyId && !!branchId,
  });

  const { data: transactions, isLoading: isTxLoading } = useQuery({
    queryKey: ['item-transactions', pharmacyId, branchId, historyItem?.medicineId],
    queryFn: () =>
      pharmacyId && branchId && historyItem
        ? pharmacyOpsApi.getTransactions(pharmacyId, branchId, historyItem.medicineId)
        : Promise.resolve([]),
    enabled: !!pharmacyId && !!branchId && !!historyItem,
  });

  const adjustMutation = useMutation({
    mutationFn: async () => {
      if (!pharmacyId || !branchId || !adjustItem) return;
      return pharmacyOpsApi.adjustStock(pharmacyId, branchId, adjustItem.medicineId, {
        deltaQuantity,
        reason: adjustReason,
        notes: adjustNotes || null,
      });
    },
    onSuccess: () => {
      setAdjustItem(null);
      setDeltaQuantity(0);
      setAdjustNotes('');
      queryClient.invalidateQueries({ queryKey: ['branch-inventory'] });
      queryClient.invalidateQueries({ queryKey: ['branch-dashboard'] });
    },
  });

  const priceMutation = useMutation({
    mutationFn: async () => {
      if (!pharmacyId || !branchId || !priceItem) return;
      return pharmacyOpsApi.updatePrice(pharmacyId, branchId, priceItem.medicineId, {
        newPrice,
      });
    },
    onSuccess: () => {
      setPriceItem(null);
      queryClient.invalidateQueries({ queryKey: ['branch-inventory'] });
    },
  });

  if (!currentBranch) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
        Please select a branch to manage inventory.
      </div>
    );
  }

  const filtered = (inventory || []).filter((item: InventoryItemDto) => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      item.medicineBrandName.toLowerCase().includes(term) ||
      item.medicineGenericName.toLowerCase().includes(term)
    );
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Branch Inventory</h1>
          <p className="text-xs text-slate-500">
            Real-time physical stock counts, reserved holds, and unit prices for {currentBranch.name}.
          </p>
        </div>
        <Button size="sm" variant="outline" onClick={() => refetch()} className="text-xs">
          Refresh Inventory
        </Button>
      </div>

      {/* Search Bar */}
      <div className="relative w-full max-w-sm">
        <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
        <Input
          placeholder="Filter medicines by name or generic..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-9 h-9 text-xs bg-white"
        />
      </div>

      {isLoading ? (
        <div className="py-16">
          <LoadingSpinner size="lg" text="Loading branch inventory items..." />
        </div>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => refetch()} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Boxes}
          title="No Inventory Records"
          description="No inventory items found matching your filter criteria."
        />
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Medicine</TableHead>
                <TableHead className="text-center">Available Units</TableHead>
                <TableHead className="text-center">Reserved Holds</TableHead>
                <TableHead className="text-center">Total Physical</TableHead>
                <TableHead className="text-right">Unit Price</TableHead>
                <TableHead className="text-center">Freshness</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item: InventoryItemDto) => (
                <TableRow key={item.id}>
                  <TableCell>
                    <div className="font-bold text-slate-900">{item.medicineBrandName}</div>
                    <div className="text-xs text-slate-500">{item.medicineGenericName}</div>
                  </TableCell>

                  <TableCell className="text-center">
                    <Badge variant={item.availableQuantity > 5 ? 'success' : item.availableQuantity > 0 ? 'warning' : 'destructive'}>
                      {item.availableQuantity} available
                    </Badge>
                  </TableCell>

                  <TableCell className="text-center font-semibold text-slate-700 text-xs">
                    {item.reservedQuantity}
                  </TableCell>

                  <TableCell className="text-center font-bold text-slate-900 text-sm">
                    {item.totalStock}
                  </TableCell>

                  <TableCell className="text-right font-bold text-slate-900">
                    {formatCurrency(item.price, item.currency)}
                  </TableCell>

                  <TableCell className="text-center">
                    <span className="text-[11px] font-mono text-slate-500 bg-slate-100 px-2 py-0.5 rounded">
                      {item.freshness}
                    </span>
                  </TableCell>

                  <TableCell className="text-right space-x-1">
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs"
                      onClick={() => {
                        setAdjustItem(item);
                        setDeltaQuantity(0);
                      }}
                      title="Adjust Stock"
                    >
                      Stock ±
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs"
                      onClick={() => {
                        setPriceItem(item);
                        setNewPrice(item.price);
                      }}
                      title="Update Price"
                    >
                      Price
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-8 px-2 text-xs text-slate-500"
                      onClick={() => setHistoryItem(item)}
                      title="View Transactions"
                    >
                      <History className="h-3.5 w-3.5" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Stock Adjustment Modal */}
      <Dialog open={!!adjustItem} onOpenChange={() => setAdjustItem(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">Adjust Physical Stock</DialogTitle>
          </DialogHeader>

          {adjustItem && (
            <div className="space-y-4 py-2">
              <div className="rounded-lg bg-slate-50 p-3 text-xs space-y-1">
                <p className="font-bold text-slate-900">{adjustItem.medicineBrandName}</p>
                <p className="text-slate-500">Current available: {adjustItem.availableQuantity} | Total: {adjustItem.totalStock}</p>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">
                  Stock Delta (+ to add, - to subtract)
                </label>
                <Input
                  type="number"
                  value={deltaQuantity}
                  onChange={(e) => setDeltaQuantity(parseInt(e.target.value) || 0)}
                  placeholder="e.g. 20 or -5"
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Adjustment Reason</label>
                <select
                  value={adjustReason}
                  onChange={(e) => setAdjustReason(e.target.value)}
                  className="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-xs"
                >
                  <option value="ReceivedShipment">Received Shipment</option>
                  <option value="ManualAdjustment">Manual Count Adjustment</option>
                  <option value="DispensedDamaged">Damaged / Expired Disposal</option>
                  <option value="ReturnFromCustomer">Customer Return</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Notes (Optional)</label>
                <Input
                  value={adjustNotes}
                  onChange={(e) => setAdjustNotes(e.target.value)}
                  placeholder="Invoice or audit reference..."
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setAdjustItem(null)}>Cancel</Button>
            <Button
              onClick={() => adjustMutation.mutate()}
              disabled={deltaQuantity === 0 || adjustMutation.isPending}
            >
              {adjustMutation.isPending ? 'Saving...' : 'Apply Stock Change'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Price Update Modal */}
      <Dialog open={!!priceItem} onOpenChange={() => setPriceItem(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">Update Medicine Price</DialogTitle>
          </DialogHeader>

          {priceItem && (
            <div className="space-y-4 py-2">
              <div className="rounded-lg bg-slate-50 p-3 text-xs">
                <p className="font-bold text-slate-900">{priceItem.medicineBrandName}</p>
                <p className="text-slate-500">Current Price: {formatCurrency(priceItem.price, priceItem.currency)}</p>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">New Unit Price ({priceItem.currency})</label>
                <Input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={newPrice}
                  onChange={(e) => setNewPrice(parseFloat(e.target.value) || 0)}
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setPriceItem(null)}>Cancel</Button>
            <Button
              onClick={() => priceMutation.mutate()}
              disabled={newPrice <= 0 || priceMutation.isPending}
            >
              {priceMutation.isPending ? 'Updating...' : 'Save New Price'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Transaction History Modal */}
      <Dialog open={!!historyItem} onOpenChange={() => setHistoryItem(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-slate-900">
              Audit History: {historyItem?.medicineBrandName}
            </DialogTitle>
          </DialogHeader>

          <div className="max-h-80 overflow-y-auto space-y-2 py-2">
            {isTxLoading ? (
              <LoadingSpinner size="sm" text="Loading transaction logs..." />
            ) : !transactions || transactions.length === 0 ? (
              <p className="text-xs text-slate-500 text-center py-6">No historical adjustments recorded.</p>
            ) : (
              transactions.map((tx: InventoryTransactionDto) => (
                <div key={tx.id} className="rounded-md border border-slate-100 bg-slate-50 p-3 text-xs flex justify-between items-center">
                  <div>
                    <span className="font-semibold text-slate-800">{tx.reason}</span>
                    <p className="text-[11px] text-slate-400">{formatDateTime(tx.createdAt)}</p>
                    {tx.notes && <p className="text-[11px] text-slate-600 italic">"{tx.notes}"</p>}
                  </div>
                  <div className="text-right">
                    <span className={`font-bold ${tx.deltaQuantity >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>
                      {tx.deltaQuantity >= 0 ? `+${tx.deltaQuantity}` : tx.deltaQuantity}
                    </span>
                    <p className="text-[10px] text-slate-400">Result: {tx.resultingQuantity}</p>
                  </div>
                </div>
              ))
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setHistoryItem(null)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
