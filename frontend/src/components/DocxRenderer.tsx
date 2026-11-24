"use client";

import { renderAsync } from "docx-preview";
import { useEffect, useRef } from "react";

export default function DocxRenderer({
  blob,
  onError,
  onRendered,
}: {
  blob: Blob;
  onError?: (message: string) => void;
  /** Reports the untransformed A4 page width so the parent can compute fit-to-width. */
  onRendered?: (width: number) => void;
}) {
  const hostRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;
    let cancelled = false;
    host.innerHTML = "";

    void renderAsync(blob, host, host, {
      className: "docx-preview",
      inWrapper: false,
      breakPages: true,
      renderHeaders: true,
      renderFooters: true,
      ignoreWidth: false,
      ignoreHeight: false,
    })
      .then(() => {
        if (cancelled) return;
        const page = host.querySelector("section") as HTMLElement | null;
        const width = page?.offsetWidth ?? host.scrollWidth;
        if (width > 0) onRendered?.(width);
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          onError?.(error instanceof Error ? error.message : "Не удалось отобразить DOCX.");
        }
      });

    return () => {
      cancelled = true;
    };
  }, [blob, onError, onRendered]);

  return <div ref={hostRef} className="docx-host" aria-label="Просмотр документа" />;
}
