"use client";

import {
  AlertCircle,
  CheckCircle2 as CheckCircle2Icon,
  Info as InfoIcon,
  RefreshCw,
  type LucideIcon,
} from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import type { PillTone } from "@/lib/statuses";

export function StatusPill({
  label,
  tone = "neutral",
  dot = true,
}: {
  label: string;
  tone?: PillTone;
  dot?: boolean;
}) {
  return (
    <span className={`pill pill-${tone}`}>
      {dot && <span className="pill-dot" aria-hidden="true" />}
      {label}
    </span>
  );
}

export function Card({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  return <section className={`card ${className}`}>{children}</section>;
}

export function PageHeader({
  title,
  subtitle,
  actions,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="page-title">{title}</h1>
        {subtitle && <p className="page-subtitle">{subtitle}</p>}
      </div>
      {actions}
    </div>
  );
}

export function Button({
  variant = "secondary",
  size = "md",
  className = "",
  icon: Icon,
  children,
  type = "button",
  ...rest
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "md" | "sm";
  icon?: LucideIcon;
}) {
  const variantClass = {
    primary: "btn-primary",
    secondary: "btn-secondary",
    ghost: "btn-ghost",
    danger: "btn-danger",
  }[variant];
  return (
    <button
      type={type}
      className={`btn ${variantClass} ${size === "sm" ? "btn-sm" : ""} ${className}`}
      {...rest}
    >
      {Icon && <Icon size={16} aria-hidden="true" />}
      {children}
    </button>
  );
}

export function SkeletonCard({ lines = 3 }: { lines?: number }) {
  return (
    <div className="card" aria-hidden="true">
      <div className="skeleton h-6 w-1/3" />
      <div className="mt-5 flex flex-col gap-3">
        {Array.from({ length: lines }).map((_, index) => (
          <div key={index} className="skeleton h-4" style={{ width: `${90 - index * 14}%` }} />
        ))}
      </div>
    </div>
  );
}

export function SkeletonTable({ rows = 4 }: { rows?: number }) {
  return (
    <div className="card" aria-hidden="true">
      <div className="skeleton h-6 w-40" />
      <div className="mt-6 flex flex-col gap-3">
        {Array.from({ length: rows }).map((_, index) => (
          <div key={index} className="flex gap-4">
            <div className="skeleton h-9 flex-1" />
            <div className="skeleton h-9 w-32" />
            <div className="skeleton h-9 w-28" />
          </div>
        ))}
      </div>
    </div>
  );
}

export function LoadingScreen() {
  return (
    <div className="flex flex-col gap-5" aria-label="Загрузка">
      <div className="skeleton h-9 w-64" />
      <div className="skeleton h-4 w-80" />
      <SkeletonCard lines={4} />
      <SkeletonTable />
    </div>
  );
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: LucideIcon;
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="empty-state">
      <div className="empty-state-icon">
        <Icon size={22} aria-hidden="true" />
      </div>
      <div className="text-[15px] font-semibold text-ink">{title}</div>
      {description && <div className="max-w-md text-[13px]">{description}</div>}
      {action}
    </div>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="card">
      <div className="alert alert-error">
        <AlertCircle size={18} className="mt-0.5 shrink-0" />
        <div className="flex-1">
          <div className="font-semibold">Не удалось загрузить данные</div>
          <div className="mt-1">{message}</div>
        </div>
      </div>
      {onRetry && (
        <div className="mt-4">
          <Button icon={RefreshCw} onClick={onRetry}>
            Повторить
          </Button>
        </div>
      )}
    </div>
  );
}

export function Alert({
  tone = "info",
  children,
}: {
  tone?: "error" | "success" | "info";
  children: ReactNode;
}) {
  const Icon = tone === "error" ? AlertCircle : tone === "success" ? CheckCircle2Icon : InfoIcon;
  return (
    <div className={`alert alert-${tone}`} role="alert">
      <Icon size={18} className="mt-0.5 shrink-0" />
      <span>{children}</span>
    </div>
  );
}

