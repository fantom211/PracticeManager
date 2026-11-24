"use client";

import { BriefcaseBusiness, Check } from "lucide-react";
import { Card, StatusPill } from "@/components/ui";
import { formatDate } from "@/lib/format";
import { participantTone } from "@/lib/statuses";
import type { PracticeOption } from "@/lib/use-practice-context";

export function PracticeContextChip({
  practiceName,
  professionalModule,
}: {
  practiceName: string;
  professionalModule: string;
}) {
  return (
    <span className="inline-flex min-w-0 items-center gap-2 rounded-full bg-primary-soft px-3 py-1.5 text-[12px] font-medium text-primary">
      <BriefcaseBusiness size={14} className="shrink-0" aria-hidden="true" />
      <span className="truncate">{practiceName}</span>
      <span className="shrink-0 opacity-70">· {professionalModule}</span>
    </span>
  );
}

export function PracticeSelector({
  options,
  selectedId,
  onSelect,
}: {
  options: PracticeOption[];
  selectedId: string | null;
  onSelect: (participantId: string) => void;
}) {
  if (options.length < 2) return null;

  return (
    <Card>
      <div className="card-header">
        <div>
          <h2 className="section-title">Выбор практики</h2>
          <p className="section-subtitle">Данные страницы соответствуют выбранной практике</p>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        {options.map((option) => {
          const selected = option.participantId === selectedId;
          return (
            <button
              key={option.participantId}
              type="button"
              aria-pressed={selected}
              onClick={() => onSelect(option.participantId)}
              className={`flex min-w-0 flex-col gap-2 rounded-xl border p-4 text-left transition-colors ${
                selected
                  ? "border-primary bg-primary-soft"
                  : "border-line bg-white hover:border-primary/40 hover:bg-[#FAFCFF]"
              }`}
            >
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    {selected && <Check size={15} className="shrink-0 text-primary" aria-hidden="true" />}
                    <span className="text-[14px] font-semibold text-ink">
                      {option.practiceName}
                    </span>
                  </div>
                  <div className="mt-0.5 text-[12px] text-muted">
                    {option.professionalModule} · {option.organization}
                  </div>
                </div>
                <StatusPill
                  label={option.statusLabel}
                  tone={participantTone(option.status)}
                />
              </div>
              <div className="text-[12px] text-muted">
                {formatDate(option.dateStart)} — {formatDate(option.dateEnd)}
              </div>
              {option.grade && (
                <div className="text-[12px] font-medium text-ink">
                  Оценка: {option.grade}
                </div>
              )}
            </button>
          );
        })}
      </div>
    </Card>
  );
}
