"use client";

import { FileText, Maximize2 } from "lucide-react";
import { useState } from "react";
import { DocumentPreviewDialog } from "@/components/DocumentPreviewDialog";
import { DocumentRenderer } from "@/components/DocumentRenderer";
import { Button, Card } from "@/components/ui";

/** Inline preview on the detail page; uses the exact same renderer as the dialog. */
export function DocumentPreview({
  previewUrl,
  downloadUrl,
  fileName,
  version,
}: {
  previewUrl: string | null;
  downloadUrl: string | null;
  fileName: string | null;
  version?: number | null;
}) {
  const [open, setOpen] = useState(false);

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <Card className="min-w-0 p-0">
        <div className="flex flex-wrap items-start justify-between gap-4 p-5">
          <div className="flex min-w-0 items-start gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-soft text-primary">
              <FileText size={22} aria-hidden="true" />
            </div>
            <div className="min-w-0">
              <h2 className="section-title text-[16px]">Предпросмотр документа</h2>
              <p className="mt-1 break-all text-[13px] text-muted">
                {fileName ?? "Файл ещё не загружен"}
              </p>
            </div>
          </div>
          <Button icon={Maximize2} variant="primary" onClick={() => setOpen(true)}>
            Открыть просмотр
          </Button>
        </div>
        <div className="h-[72vh] min-h-[520px] border-t border-line">
          <DocumentRenderer
            previewUrl={previewUrl}
            downloadUrl={downloadUrl}
            fileName={fileName}
            version={version}
          />
        </div>
      </Card>

      <DocumentPreviewDialog
        open={open}
        onClose={() => setOpen(false)}
        previewUrl={previewUrl}
        downloadUrl={downloadUrl}
        fileName={fileName}
        version={version}
      />
    </div>
  );
}
