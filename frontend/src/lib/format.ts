const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

const dateTimeFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const numberFormatter = new Intl.NumberFormat("ru-RU");

/** DateOnly values arrive as `YYYY-MM-DD`; parse as local noon to avoid UTC day shifts. */
function parseDate(value: string): Date | null {
  if (!value) return null;
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (match) {
    return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 12);
  }
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function formatDate(value: string | null | undefined): string {
  const date = value ? parseDate(value) : null;
  return date ? dateFormatter.format(date) : "—";
}

export function formatDateTime(value: string | null | undefined): string {
  const date = value ? parseDate(value) : null;
  return date ? dateTimeFormatter.format(date) : "—";
}

export function formatNumber(value: number): string {
  return numberFormatter.format(value);
}

export function formatBytes(value: number): string {
  if (value < 1024) return `${value} Б`;
  if (value < 1024 * 1024) return `${numberFormatter.format(Math.round(value / 1024))} КБ`;
  return `${numberFormatter.format(Math.round(value / (1024 * 1024)))} МБ`;
}

export function toDateInput(value: string | null | undefined): string {
  if (!value) return "";
  return value.slice(0, 10);
}

export function relativeDays(daysLeft: number): string {
  if (daysLeft < 0) return `просрочено на ${Math.abs(daysLeft)} дн.`;
  if (daysLeft === 0) return "сегодня";
  if (daysLeft === 1) return "до 1 дня";
  if (daysLeft < 5) return `до ${daysLeft} дней`;
  return `до ${daysLeft} дней`;
}

export function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "??";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}
