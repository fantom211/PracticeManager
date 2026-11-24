"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Modal } from "@/components/Modal";
import { useToast } from "@/components/ToastProvider";
import { Alert, Button } from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { withParticipant } from "@/lib/navigation";

/** Rendered only for documents whose API payload has `canDelete === true`. */
export function DeleteDocumentButton({
  documentId,
  documentName,
  participantId,
  navigateOnSuccess = false,
  variant = "button",
}: {
  documentId: string;
  documentName: string;
  participantId?: string | null;
  navigateOnSuccess?: boolean;
  variant?: "button" | "icon";
}) {
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const router = useRouter();
  const { notify } = useToast();

  const mutation = useMutation({
    mutationFn: () => api.deleteDocument(documentId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.document(documentId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.documents }),
        queryClient.invalidateQueries({ queryKey: queryKeys.practices }),
        queryClient.invalidateQueries({ queryKey: queryKeys.notifications }),
      ]);
      notify("Документ удалён.", "success");
      setError(null);
      setOpen(false);
      if (navigateOnSuccess) {
        router.push(withParticipant("/documents", participantId));
      }
    },
    onError: (reason: unknown) => setError(errorMessage(reason)),
  });

  const close = () => {
    if (mutation.isPending) return;
    setError(null);
    setOpen(false);
  };

  return (
    <>
      {variant === "icon" ? (
        <button
          type="button"
          className="icon-btn text-danger"
          aria-label={`Удалить документ ${documentName}`}
          title="Удалить документ"
          onClick={() => setOpen(true)}
        >
          <Trash2 size={16} aria-hidden="true" />
        </button>
      ) : (
        <Button
          icon={Trash2}
          variant="danger"
          disabled={mutation.isPending}
          onClick={() => setOpen(true)}
        >
          Удалить документ
        </Button>
      )}

      <Modal
        open={open}
        onClose={close}
        title="Удаление документа"
        footer={
          <>
            <Button
              variant="danger"
              icon={Trash2}
              disabled={mutation.isPending}
              onClick={() => mutation.mutate()}
            >
              Удалить
            </Button>
            <Button variant="secondary" disabled={mutation.isPending} onClick={close}>
              Отмена
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-3">
          <p className="text-[15px] font-semibold text-ink">
            Удалить документ «{documentName}»?
          </p>
          <p className="text-[13px] text-muted">
            Документ исчезнет из списка и больше не будет доступен для просмотра.
          </p>
          {error && <Alert tone="error">{error}</Alert>}
        </div>
      </Modal>
    </>
  );
}
