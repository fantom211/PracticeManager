"use client";

import { X } from "lucide-react";
import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useSyncExternalStore,
  type ReactNode,
  type RefObject,
} from "react";
import { createPortal } from "react-dom";

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

const subscribeNoop = () => () => {};

/**
 * Shared accessible modal primitive. Used by the preview, comment and delete dialogs.
 * The default classes match the student UI; callers may override each layer.
 */
export function Modal({
  open,
  onClose,
  title,
  children,
  footer,
  overlayClassName = "fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4",
  panelClassName = "flex max-h-[92vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl",
  headerClassName = "flex shrink-0 items-center justify-between gap-4 border-b border-line px-5 py-3",
  bodyClassName = "min-h-0 flex-1 overflow-auto p-5",
  footerClassName = "flex shrink-0 items-center justify-end gap-2 border-t border-line px-5 py-3",
  bodyTestId,
  bodyRef,
  closeLabel = "Закрыть",
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
  overlayClassName?: string;
  panelClassName?: string;
  headerClassName?: string;
  bodyClassName?: string;
  footerClassName?: string;
  bodyTestId?: string;
  bodyRef?: RefObject<HTMLDivElement | null>;
  closeLabel?: string;
}) {
  const mounted = useSyncExternalStore(
    subscribeNoop,
    () => true,
    () => false,
  );
  const panelRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);
  const titleId = useId();

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab") return;
      const panel = panelRef.current;
      if (!panel) return;
      const focusables = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
        (element) => element.offsetParent !== null || element === document.activeElement,
      );
      if (focusables.length === 0) {
        event.preventDefault();
        panel.focus();
        return;
      }
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const active = document.activeElement;
      if (event.shiftKey && (active === first || active === panel)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    },
    [],
  );

  useEffect(() => {
    if (!open || !mounted) return;
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    panelRef.current?.focus();
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus();
    };
  }, [handleKeyDown, mounted, open]);

  if (!open || !mounted) return null;

  return createPortal(
    <div className={overlayClassName} role="presentation">
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={panelClassName}
      >
        <header className={headerClassName}>
          <h2 id={titleId} className="truncate text-[16px] font-semibold text-ink">
            {title}
          </h2>
          <button
            type="button"
            aria-label={closeLabel}
            onClick={onClose}
            className="shrink-0 rounded-lg p-1.5 text-muted transition-colors hover:bg-neutral-soft hover:text-ink"
          >
            <X size={18} aria-hidden="true" />
          </button>
        </header>
        <div ref={bodyRef} data-testid={bodyTestId} className={bodyClassName}>
          {children}
        </div>
        {footer && <footer className={footerClassName}>{footer}</footer>}
      </div>
    </div>,
    document.body,
  );
}
