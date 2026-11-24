"use client";

import { DocumentRenderer } from "@/components/DocumentRenderer";
import { Modal } from "@/components/Modal";

export interface DocumentPreviewDialogProps {
  open: boolean;
  onClose: () => void;
  previewUrl: string | null;
  downloadUrl: string | null;
  fileName: string | null;
  version?: number | null;
  title?: string;
}

/** Full-screen preview shell; all rendering lives in the shared DocumentRenderer. */
export function DocumentPreviewDialog({
  open,
  onClose,
  previewUrl,
  downloadUrl,
  fileName,
  version,
  title,
}: DocumentPreviewDialogProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title ?? fileName ?? "Просмотр документа"}
      overlayClassName="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4"
      panelClassName="flex h-[92vh] w-full max-w-[1400px] flex-col overflow-hidden rounded-2xl bg-white"
      headerClassName="flex shrink-0 items-center justify-between border-b border-line px-5 py-3"
      bodyClassName="min-h-0 flex-1 overflow-hidden p-0"
    >
      <DocumentRenderer
        previewUrl={previewUrl}
        downloadUrl={downloadUrl}
        fileName={fileName}
        version={version}
        viewportTestId="document-preview-viewport"
      />
    </Modal>
  );
}
