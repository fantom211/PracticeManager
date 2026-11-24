"use client";

import { useQuery } from "@tanstack/react-query";
import { GraduationCap, Mail, Phone, User as UserIcon } from "lucide-react";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { initials } from "@/lib/format";
import { ClientOnly } from "@/components/ClientOnly";
import { Card, ErrorState, PageHeader, SkeletonCard } from "@/components/ui";

export default function ProfilePage() {
  const fallback = (
    <div className="flex flex-col gap-5">
      <div className="skeleton h-9 w-48" />
      <SkeletonCard lines={5} />
    </div>
  );

  return (
    <ClientOnly fallback={fallback}>
      <ProfileContent />
    </ClientOnly>
  );
}

function ProfileContent() {
  const query = useQuery({ queryKey: queryKeys.me, queryFn: api.me });

  if (query.isError) {
    return <ErrorState message={errorMessage(query.error)} onRetry={() => void query.refetch()} />;
  }
  if (query.isPending || !query.data) {
    return (
      <div className="flex flex-col gap-5">
        <div className="skeleton h-9 w-48" />
        <SkeletonCard lines={5} />
      </div>
    );
  }

  const me = query.data;

  return (
    <>
      <PageHeader title="Профиль" subtitle="Сведения о студенте" />

      <Card>
        <div className="flex flex-wrap items-center gap-5 border-b border-line pb-6">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary-soft text-[20px] font-bold text-primary">
            {initials(me.fullName)}
          </div>
          <div>
            <h2 className="text-[20px] font-semibold text-ink">{me.fullName}</h2>
            <p className="mt-1 text-[13px] text-muted">
              {me.group} · {me.specialtyCode}
            </p>
          </div>
        </div>

        <div className="mt-6 grid gap-x-8 gap-y-5 sm:grid-cols-2">
          <Field icon={GraduationCap} label="Группа" value={me.group} />
          <Field icon={GraduationCap} label="Специальность" value={`${me.specialtyCode} ${me.specialtyName}`} />
          <Field icon={UserIcon} label="Номер студенческого билета" value={me.studentNumber} />
          <Field icon={GraduationCap} label="Курс" value={`${me.course}`} />
          <Field icon={Phone} label="Телефон" value={me.phone ?? "—"} />
          <Field icon={Mail} label="Электронная почта" value={me.email ?? "—"} />
        </div>
      </Card>
    </>
  );
}

function Field({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof UserIcon;
  label: string;
  value: string;
}) {
  return (
    <div>
      <div className="flex items-center gap-2 text-[12px] text-muted">
        <Icon size={15} className="text-primary" aria-hidden="true" />
        {label}
      </div>
      <div className="mt-1 text-[14px] font-medium text-ink">{value}</div>
    </div>
  );
}
