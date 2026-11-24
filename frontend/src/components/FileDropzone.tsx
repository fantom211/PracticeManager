"use client";

import { FileText, UploadCloud, X } from "lucide-react";
import { useRef, useState } from "react";
import { formatBytes } from "@/lib/format";

export function FileDropzone({
  label,
  file,
  onFile,
}: {
  label: string;
  file: File | null;
  onFile: (file: File | null) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);

  return (
    <div>
      <span className="label">{label}</span>
      <div
        className={`dropzone ${dragging ? "dropzone-active" : ""}`}
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            inputRef.current?.click();
          }
        }}
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          const dropped = event.dataTransfer.files?.[0];
          if (dropped) onFile(dropped);
        }}
      >
        <UploadCloud size={26} aria-hidden="true" />
        <span className="text-[13px] font-semibold">Перетащите файл сюда или выберите</span>
        <span className="text-[12px]">DOCX или PDF, не более 10 МБ</span>
        <input
          ref={inputRef}
          type="file"
          accept=".docx,.pdf"
          className="hidden"
          onChange={(event) => {
            onFile(event.target.files?.[0] ?? null);
            event.target.value = "";
          }}
        />
      </div>
      {file && (
        <div className="mt-3 flex items-center gap-3 rounded-lg border border-line bg-table-head px-3 py-2">
          <FileText size={16} className="shrink-0 text-primary" aria-hidden="true" />
          <span className="min-w-0 flex-1 truncate text-[13px]">{file.name}</span>
          <span className="shrink-0 text-[12px] text-muted">{formatBytes(file.size)}</span>
          <button
            type="button"
            className="icon-btn h-7 w-7"
            aria-label="Удалить файл"
            onClick={() => onFile(null)}
          >
            <X size={14} />
          </button>
        </div>
      )}
    </div>
  );
}
