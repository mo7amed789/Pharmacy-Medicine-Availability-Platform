/** Egyptian mobile: 01[0125] + 8 digits, optional +20 / 20 prefix. Mirrors the server rule. */
export function normalizeEgyptPhone(input: string): string | null {
  const cleaned = input.replace(/[\s\-()]/g, '');
  const m = /^(?:\+?20|0)?(1[0125][0-9]{8})$/.exec(cleaned);
  return m ? `+20${m[1]}` : null;
}
