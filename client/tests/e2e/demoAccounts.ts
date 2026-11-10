/** Test identities exist in both the 200-user and legacy development datasets. */
export function demoUsername(username: string): string {
  return username.replace(/^Player_(\d{4})$/, (_, digits: string) => `Player_${String(Number(digits) % 200).padStart(4, '0')}`);
}
