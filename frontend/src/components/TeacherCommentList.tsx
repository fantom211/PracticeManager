"use client";

import { ChevronRight } from "lucide-react";
import { useMemo, useState } from "react";
import { Modal } from "@/components/Modal";
import { Button, StatusPill } from "@/components/ui";
import { formatDateTime, initials } from "@/lib/format";
import type { PillTone } from "@/lib/statuses";
import type { TeacherCommentDto } from "@/lib/types";

const kindMeta: Record<
  TeacherCommentDto["kind"],
  { label: string; tone: PillTone }
> = {
  return: { label: "Возврат", tone: "danger" },
  acceptance: { label: "Принято", tone: "success" },
  clarification: { label: "Замечание", tone: "warning" },
};

function shortAuthorName(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length <= 1) return fullName || "Преподаватель";
  return `${parts[0]} ${parts.slice(1).map((part) => `${part[0]}.`).join(" ")}`;
}

type TextBlock = { kind: "p" | "ul"; lines: string[] };

/** Keeps paragraph and bullet structure of seeded full comments without HTML injection. */
export function parseCommentText(text: string): TextBlock[] {
  const blocks: TextBlock[] = [];
  let paragraph: string[] = [];
  let bullets: string[] = [];

  const flushParagraph = () => {
    if (paragraph.length === 0) return;
    blocks.push({ kind: "p", lines: [paragraph.join(" ")] });
    paragraph = [];
  };
  const flushBullets = () => {
    if (bullets.length === 0) return;
    blocks.push({ kind: "ul", lines: [...bullets] });
    bullets = [];
  };

  text.replace(/\r\n/g, "\n").split("\n").forEach((line) => {
    const trimmed = line.trim();
    const bulletMatch = /^(?:[-*•]|\d+[.)])\s+(.*)$/.exec(trimmed);
    if (bulletMatch) {
      flushParagraph();
      bullets.push(bulletMatch[1]);
    } else if (!trimmed) {
      flushParagraph();
      flushBullets();
    } else {
      flushBullets();
      paragraph.push(trimmed);
    }
  });
  flushParagraph();
  flushBullets();
  return blocks;
}

export function TeacherCommentList({
  comments,
  documentName,
  documentVersion,
  isReturned,
  legacyComment,
  legacyCreatedAt,
}: {
  comments: TeacherCommentDto[];
  documentName: string;
  documentVersion: number | null;
  isReturned: boolean;
  legacyComment?: string | null;
  legacyCreatedAt?: string | null;
}) {
  const [activeComment, setActiveComment] = useState<TeacherCommentDto | null>(null);

  const items = useMemo(() => {
    const source = [...comments].sort(
      (left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt),
    );
    if (source.length > 0) return source;
    if (!legacyComment) return [];
    return [
      {
        id: `legacy-${documentName}`,
        documentId: "",
        versionNumber: documentVersion,
        authorName: "Преподаватель",
        authorRole: "Преподаватель",
        kind: isReturned ? "return" : "clarification",
        summary: legacyComment,
        fullText: legacyComment,
        createdAt: legacyCreatedAt ?? "2026-01-01T00:00:00Z",
      } satisfies TeacherCommentDto,
    ];
  }, [comments, documentName, documentVersion, isReturned, legacyComment, legacyCreatedAt]);

  const blocks = useMemo(
    () => (activeComment ? parseCommentText(activeComment.fullText) : []),
    [activeComment],
  );
  const newestReturnId = useMemo(
    () => items.find((item) => item.kind === "return")?.id ?? null,
    [items],
  );

  if (items.length === 0) {
    return <p className="mt-3 text-[13px] text-muted">Комментариев нет.</p>;
  }

  return (
    <>
      <div className="flex flex-col gap-3">
        {items.map((comment) => {
          const meta = kindMeta[comment.kind] ?? kindMeta.clarification;
          const highlighted = isReturned && comment.id === newestReturnId;
          const expanded = activeComment?.id === comment.id;
          return (
            <article
              key={comment.id}
              className={`rounded-xl border p-4 ${
                highlighted
                  ? "border-danger/25 bg-[#FEF2F2]"
                  : "border-line bg-white"
              }`}
            >
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="flex min-w-0 items-center gap-2">
                  <span
                    className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-[11px] font-bold ${
                      highlighted
                        ? "bg-[#FEE2E2] text-danger"
                        : "bg-primary-soft text-primary"
                    }`}
                    aria-hidden="true"
                  >
                    {initials(comment.authorName)}
                  </span>
                  <div className="min-w-0">
                    <div className="truncate text-[13px] font-semibold text-ink">
                      {shortAuthorName(comment.authorName)}
                    </div>
                    <div className="text-[12px] text-muted">{comment.authorRole}</div>
                  </div>
                </div>
                <StatusPill label={meta.label} tone={meta.tone} dot={false} />
              </div>
              <div className="mt-2 text-[11px] text-muted">
                {formatDateTime(comment.createdAt)}
                {" · "}
                {documentName}
                {comment.versionNumber ? ` · v${comment.versionNumber}` : ""}
              </div>
              <p className="mt-3 line-clamp-3 text-[13px] leading-relaxed text-ink">
                {comment.summary}
              </p>
              <button
                type="button"
                aria-expanded={expanded}
                onClick={() => setActiveComment(comment)}
                className="mt-3 inline-flex items-center gap-1 text-[12px] font-semibold text-primary hover:text-primary-hover"
              >
                Читать полностью <ChevronRight size={14} aria-hidden="true" />
              </button>
            </article>
          );
        })}
      </div>

      <Modal
        open={activeComment !== null}
        onClose={() => setActiveComment(null)}
        title="Комментарий преподавателя"
        footer={
          <Button variant="secondary" onClick={() => setActiveComment(null)}>
            Закрыть
          </Button>
        }
      >
        {activeComment && (
          <div className="flex flex-col gap-5">
            <div className="grid grid-cols-1 gap-3 rounded-xl bg-[#FAFCFF] p-4 sm:grid-cols-2">
              <MetaRow label="Автор" value={activeComment.authorName} />
              <MetaRow label="Роль" value={activeComment.authorRole} />
              <MetaRow label="Дата" value={formatDateTime(activeComment.createdAt)} />
              <MetaRow
                label="Версия"
                value={activeComment.versionNumber ? `v${activeComment.versionNumber}` : "—"}
              />
              <MetaRow label="Документ" value={documentName} wide />
            </div>
            <div className="flex flex-col gap-3 text-[14px] leading-relaxed text-ink">
              {blocks.map((block, index) =>
                block.kind === "p" ? (
                  <p key={index}>{block.lines[0]}</p>
                ) : (
                  <ul key={index} className="list-disc space-y-1 pl-5">
                    {block.lines.map((line, lineIndex) => (
                      <li key={lineIndex}>{line}</li>
                    ))}
                  </ul>
                ),
              )}
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}

function MetaRow({
  label,
  value,
  wide = false,
}: {
  label: string;
  value: string;
  wide?: boolean;
}) {
  return (
    <div className={wide ? "sm:col-span-2" : undefined}>
      <div className="text-[11px] font-medium uppercase tracking-wide text-muted">
        {label}
      </div>
      <div className="mt-0.5 break-words text-[13px] font-medium text-ink">{value}</div>
    </div>
  );
}
