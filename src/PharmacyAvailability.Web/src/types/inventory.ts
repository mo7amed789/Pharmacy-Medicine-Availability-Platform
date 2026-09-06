export interface InventoryItemDto {
  id: string;
  pharmacyId: string;
  branchId: string;
  medicineId: string;
  medicineBrandName: string;
  medicineGenericName: string;
  availableQuantity: number;
  reservedQuantity: number;
  totalStock: number;
  price: number;
  currency: string;
  source: string;
  freshness: string;
  updatedAt: string;
}

export interface CreateInventoryItemRequest {
  medicineId: string;
  initialQuantity: number;
  price: number;
  currency?: string;
}

export interface AdjustStockRequest {
  deltaQuantity: number;
  reason?: string;
  notes?: string | null;
}

export interface UpdatePriceRequest {
  newPrice: number;
}

export interface InventoryTransactionDto {
  id: string;
  pharmacyInventoryId: string;
  deltaQuantity: number;
  resultingQuantity: number;
  reason: string;
  actorUserId: string;
  notes: string | null;
  createdAt: string;
}

export interface PriceHistoryDto {
  id: string;
  pharmacyInventoryId: string;
  oldPrice: number;
  newPrice: number;
  changedByUserId: string;
  createdAt: string;
}
