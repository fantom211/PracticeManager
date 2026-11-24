"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Bell,
  CalendarDays as CalendarDaysIcon,
  ClipboardList,
  FileDown,
  FileText,
  GraduationCap,
  LogOut,
  TrendingUp,
  User,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import type { ReactNode } from "react";
import { api, queryKeys } from "@/lib/api";
import { initials } from "@/lib/format";
import { withParticipant } from "@/lib/navigation";

const navItems: { href: string; label: string; icon: LucideIcon }[] = [
  { href: "/practice", label: "Моя практика", icon: ClipboardList },
  { href: "/documents", label: "Документы", icon: FileText },
  { href: "/templates", label: "Шаблоны документов", icon: FileDown },
  { href: "/tasks", label: "Задания", icon: CalendarDaysIcon },
  { href: "/notifications", label: "Уведомления", icon: Bell },
  { href: "/progress", label: "Успеваемость", icon: TrendingUp },
  { href: "/profile", label: "Профиль", icon: User },
];

const pageMeta: Record<string, { title: string; subtitle: string }> = {
  "/practice": {
    title: "Моя практика",
    subtitle: "Актуальная информация о практике и документах",
  },
  "/documents": {
    title: "Документы",
    subtitle: "Отчётность по практике, статусы и версии файлов",
  },
  "/templates": {
    title: "Шаблоны документов",
    subtitle: "Действующие шаблоны DOCX для скачивания",
  },
  "/tasks": {
    title: "Задания",
    subtitle: "Задания на практику от руководителя",
  },
  "/notifications": {
    title: "Уведомления",
    subtitle: "Новые события по вашей практике",
  },
  "/progress": {
    title: "Успеваемость",
    subtitle: "Результаты практики и выполнение заданий",
  },
  "/profile": {
    title: "Профиль",
    subtitle: "Сведения о студенте",
  },
};

function metaFor(pathname: string) {
  if (pageMeta[pathname]) return pageMeta[pathname];
  if (pathname.startsWith("/documents/")) {
    return {
      title: "Документы",
      subtitle: "Просмотр, генерация и загрузка версий",
    };
  }
  return { title: "Личный кабинет", subtitle: "Студенческая практика" };
}

const participantPaths = new Set([
  "/practice",
  "/documents",
  "/templates",
  "/tasks",
]);

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const participantId = searchParams.get("participantId");
  const meta = metaFor(pathname);

  const hrefFor = (href: string) =>
    participantPaths.has(href) ? withParticipant(href, participantId) : href;

  const { data: me } = useQuery({
    queryKey: queryKeys.me,
    queryFn: api.me,
    staleTime: Infinity,
  });
  const { data: notifications } = useQuery({
    queryKey: queryKeys.notifications,
    queryFn: api.notifications,
  });
  const unread = notifications?.filter((item) => !item.isRead).length ?? 0;

  return (
    <div className="shell">
      <aside className="sidebar" aria-label="Основная навигация">
        <div className="sidebar-logo">
          <div className="logo-tile">
            <GraduationCap size={22} aria-hidden="true" />
          </div>
          <div className="logo-text">
            <span>Личный кабинет</span>
            <span>студента</span>
          </div>
        </div>

        <nav className="nav-list">
          {navItems.map(({ href, label, icon: Icon }) => {
            const active = pathname === href || pathname.startsWith(`${href}/`);
            return (
              <Link
                key={href}
                href={hrefFor(href)}
                className={`nav-item ${active ? "nav-item-active" : ""}`}
                aria-current={active ? "page" : undefined}
              >
                <Icon size={18} aria-hidden="true" />
                <span>{label}</span>
                {href === "/notifications" && unread > 0 && (
                  <span className="nav-badge" aria-label={`${unread} непрочитанных`}>
                    {unread}
                  </span>
                )}
              </Link>
            );
          })}
        </nav>

        <div className="sidebar-footer">
          <button
            type="button"
            className="nav-item w-full"
            disabled
            title="Выход недоступен в демо-режиме"
          >
            <LogOut size={18} aria-hidden="true" />
            <span>Выйти</span>
          </button>
        </div>
      </aside>

      <div className="main-area">
        <header className="topbar">
          <div className="min-w-0">
            <h2 className="truncate text-[17px] font-semibold text-ink">{meta.title}</h2>
            <p className="topbar-subtitle truncate text-[12px] text-muted">{meta.subtitle}</p>
          </div>
          <div className="topbar-actions">
            <Link
              href="/notifications"
              className="icon-btn bell-wrap"
              aria-label={`Уведомления${unread ? `, непрочитанных: ${unread}` : ""}`}
            >
              <Bell size={18} aria-hidden="true" />
              {unread > 0 && <span className="bell-dot">{unread}</span>}
            </Link>
            <Link href="/profile" aria-label="Открыть профиль" className="flex items-center gap-2">
              <span className="avatar">{initials(me?.shortName ?? me?.fullName ?? "Студент")}</span>
            </Link>
          </div>
        </header>
        <main className="content">{children}</main>
      </div>
    </div>
  );
}
