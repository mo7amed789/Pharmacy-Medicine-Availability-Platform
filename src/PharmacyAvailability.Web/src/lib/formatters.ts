import { format, formatDistanceToNow } from 'date-fns';

export function formatCurrency(amount: number, currency: string = 'EGP'): string {
  return `${amount.toFixed(2)} ${currency}`;
}

export function formatDistance(distanceKm: number | null | undefined): string {
  if (distanceKm == null) return 'Distance unavailable';
  if (distanceKm < 1) {
    return `${Math.round(distanceKm * 1000)} m`;
  }
  return `${distanceKm.toFixed(1)} km`;
}

export function formatDateTime(isoString: string | null | undefined): string {
  if (!isoString) return '-';
  try {
    return format(new Date(isoString), 'MMM dd, yyyy HH:mm');
  } catch {
    return isoString;
  }
}

export function formatRelativeTime(isoString: string | null | undefined): string {
  if (!isoString) return '-';
  try {
    return formatDistanceToNow(new Date(isoString), { addSuffix: true });
  } catch {
    return isoString;
  }
}
