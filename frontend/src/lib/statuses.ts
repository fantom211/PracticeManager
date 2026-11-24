export type PillTone =
  | "neutral"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "purple";

const documentTones: Record<string, PillTone> = {
  notUploaded: "neutral",
  draft: "neutral",
  submitted: "warning",
  returned: "danger",
  accepted: "success",
};

const participantTones: Record<string, PillTone> = {
  assigned: "info",
  notStarted: "neutral",
  inProgress: "success",
  completed: "info",
  failed: "danger",
};

const stageTones: Record<string, PillTone> = {
  notStarted: "neutral",
  inProgress: "success",
  completed: "info",
};

const taskTones: Record<string, PillTone> = {
  assigned: "info",
  inProgress: "success",
  submitted: "warning",
  accepted: "success",
  returned: "danger",
};

const sourceTones: Record<string, PillTone> = {
  seed: "purple",
  generated: "purple",
  uploaded: "info",
};

function tone(map: Record<string, PillTone>, status: string): PillTone {
  return map[status] ?? "neutral";
}

export const documentTone = (status: string) => tone(documentTones, status);
export const participantTone = (status: string) => tone(participantTones, status);
export const stageTone = (status: string) => tone(stageTones, status);
export const taskTone = (status: string) => tone(taskTones, status);
export const sourceTone = (status: string) => tone(sourceTones, status);

export const documentFilters: { key: string; label: string }[] = [
  { key: "all", label: "Все" },
  { key: "notUploaded", label: "Не загружены" },
  { key: "submitted", label: "На проверке" },
  { key: "returned", label: "Возвращены" },
  { key: "accepted", label: "Приняты" },
];

export const taskFilters: { key: string; label: string }[] = [
  { key: "all", label: "Все" },
  { key: "assigned", label: "Назначено" },
  { key: "inProgress", label: "В процессе" },
  { key: "submitted", label: "На проверке" },
  { key: "accepted", label: "Принято" },
  { key: "returned", label: "Возвращено" },
];
