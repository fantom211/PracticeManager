/** Append the selected practice participant to internal links without duplicating query keys. */
export function withParticipant(path: string, participantId?: string | null): string {
  if (!participantId) return path;
  const separator = path.includes("?") ? "&" : "?";
  return `${path}${separator}participantId=${encodeURIComponent(participantId)}`;
}
