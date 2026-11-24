"use client";

import { Download, FileWarning } from "lucide-react";
import dynamic from "next/dynamic";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Alert, EmptyState } from "@/components/ui";
import { errorMessage, fetchBlob } from "@/lib/api";

const DocxRenderer = dynamic(() => import("@/components/DocxRenderer"), {
  ssr: false,
  loading: () => <div className="skeleton h-24 w-full" aria-hidden="true" />,
});

type ZoomState = { mode: "fit" } | { mode: "custom"; value: number };

export interface DocumentRendererProps {
  previewUrl: string | null;
  downloadUrl: string | null;
  fileName: string | null;
  version?: number | null;
  /** Extra classes for the scrolling viewport (inline preview controls its own height). */
  viewportClassName?: string;
  viewportTestId?: string;
}

/**
 * Single DOCX/PDF rendering implementation shared by the inline preview and the
 * full-screen dialog. The ResizeObserver fit-to-width scale is calculated from
 * the untransformed docx-preview output; horizontal scrolling is never clipped.
 */
export function DocumentRenderer({
  previewUrl,
  downloadUrl,
  fileName,
  version,
  viewportClassName = "",
  viewportTestId,
}: DocumentRendererProps) {
  const versionedPreviewUrl = useMemo(() => {
    if (!previewUrl || !version || previewUrl.includes("version=")) return previewUrl;
    const separator = previewUrl.includes("?") ? "&" : "?";
    return `${previewUrl}${separator}version=${version}`;
  }, [previewUrl, version]);

  // Keying the content by URL resets loading/zoom state without setState-in-effect.
  return (
    <DocumentRendererContent
      key={`${versionedPreviewUrl ?? "empty"}|${fileName ?? ""}`}
      versionedPreviewUrl={versionedPreviewUrl}
      downloadUrl={downloadUrl}
      fileName={fileName}
      viewportClassName={viewportClassName}
      viewportTestId={viewportTestId}
    />
  );
}

function DocumentRendererContent({
  versionedPreviewUrl,
  downloadUrl,
  fileName,
  viewportClassName,
  viewportTestId,
}: {
  versionedPreviewUrl: string | null;
  downloadUrl: string | null;
  fileName: string | null;
  viewportClassName: string;
  viewportTestId?: string;
}) {
  const [blob, setBlob] = useState<Blob | null>(null);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(Boolean(versionedPreviewUrl));
  const [naturalWidth, setNaturalWidth] = useState(0);
  const [viewportWidth, setViewportWidth] = useState(0);
  const [zoom, setZoom] = useState<ZoomState>({ mode: "fit" });
  const viewportMeasureRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!versionedPreviewUrl) return;
    let cancelled = false;
    void fetchBlob(versionedPreviewUrl)
      .then((result) => {
        if (cancelled) return;
        const isPdf =
          result.type === "application/pdf" ||
          (fileName ?? "").toLowerCase().endsWith(".pdf");
        if (isPdf) {
          setPdfUrl(URL.createObjectURL(result));
        } else {
          setBlob(result);
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) setError(errorMessage(reason));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [fileName, versionedPreviewUrl]);

  useEffect(() => {
    return () => {
      if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    };
  }, [pdfUrl]);

  useEffect(() => {
    const element = viewportMeasureRef.current;
    if (!element || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver((entries) => {
      setViewportWidth(entries[0]?.contentRect.width ?? 0);
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const fitScale =
    naturalWidth > 0 && viewportWidth > 0
      ? Math.min(1, viewportWidth / naturalWidth)
      : 1;
  const scale = zoom.mode === "fit" ? fitScale : zoom.value;
  const handleRenderError = useCallback(
    (message: string) => setError(message),
    [],
  );
  const handleDocxRendered = useCallback((width: number) => {
    setNaturalWidth(width);
  }, []);

  const changeZoom = (delta: number) => {
    setZoom({
      mode: "custom",
      value: Math.max(0.3, Number((scale + delta).toFixed(2))),
    });
  };

  return (
    <div className="flex h-full min-h-0 w-full flex-col bg-white">
      <div
        ref={viewportMeasureRef}
        data-testid={viewportTestId}
        className={`min-h-0 flex-1 overflow-auto bg-slate-100 p-4 ${viewportClassName}`}
      >
        <div className="mx-auto flex min-h-full w-full min-w-0 flex-col items-center">
          {!versionedPreviewUrl ? (
            <EmptyState
              icon={FileWarning}
              title="Предпросмотр недоступен"
              description="Загрузите версию документа или сгенерируйте его из шаблона."
            />
          ) : loading ? (
            <div className="skeleton h-[620px] w-full max-w-[820px]" aria-hidden="true" />
          ) : error ? (
            <Alert tone="error">{error}</Alert>
          ) : pdfUrl ? (
            <div className="h-full min-h-[60vh] w-full">
              <iframe
                src={pdfUrl}
                title="Предпросмотр PDF"
                className="h-full w-full border-0"
              />
            </div>
          ) : blob ? (
            <div
              className="mx-auto"
              style={{
                width: naturalWidth > 0 ? `${naturalWidth * scale}px` : "100%",
              }}
            >
              <div
                style={{
                  width: naturalWidth > 0 ? `${naturalWidth}px` : "100%",
                  transform: `scale(${scale})`,
                  transformOrigin: "top left",
                }}
              >
                <DocxRenderer
                  blob={blob}
                  onError={handleRenderError}
                  onRendered={handleDocxRendered}
                />
              </div>
            </div>
          ) : null}
        </div>
      </div>

      <div className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-t border-line bg-white px-5 py-3">
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => setZoom({ mode: "fit" })}
            className={`btn btn-sm ${zoom.mode === "fit" ? "btn-primary" : "btn-secondary"}`}
            aria-pressed={zoom.mode === "fit"}
          >
            По ширине
          </button>
          <button
            type="button"
            onClick={() => setZoom({ mode: "custom", value: 1 })}
            className={`btn btn-sm ${zoom.mode === "custom" && zoom.value === 1 ? "btn-primary" : "btn-secondary"}`}
            aria-pressed={zoom.mode === "custom" && zoom.value === 1}
          >
            100%
          </button>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            aria-label="Увеличить масштаб"
            onClick={() => changeZoom(0.1)}
          >
            +
          </button>
          <span className="min-w-[48px] text-center text-[12px] tabular-nums text-muted">
            {Math.round(scale * 100)}%
          </span>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            aria-label="Уменьшить масштаб"
            onClick={() => changeZoom(-0.1)}
          >
            −
          </button>
        </div>
        {downloadUrl && (
          <a href={downloadUrl} download className="btn btn-primary btn-sm">
            <Download size={15} aria-hidden="true" /> Скачать
          </a>
        )}
      </div>
    </div>
  );
}
