"use client";

import { useQuery } from "@tanstack/react-query";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo } from "react";
import { api, queryKeys } from "@/lib/api";
import type { PracticeCurrent, PracticeHistoryItem } from "@/lib/types";

export interface PracticeOption {
  participantId: string;
  practiceName: string;
  professionalModule: string;
  status: string;
  statusLabel: string;
  dateStart: string;
  dateEnd: string;
  organization: string;
  grade: string | null;
}

/**
 * Shared practice-selection state. The original current practice is the default;
 * a `participantId` query parameter switches to any other practice of this student.
 * Components using this hook must sit below a Suspense boundary (useSearchParams).
 */
export function usePracticeContext() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const practicesQuery = useQuery({
    queryKey: queryKeys.practices,
    queryFn: api.practices,
  });

  const current = practicesQuery.data?.current ?? null;
  const history = useMemo(
    () => practicesQuery.data?.history ?? [],
    [practicesQuery.data],
  );
  const requestedId = searchParams.get("participantId");
  const currentId = current?.participantId ?? null;

  const knownIds = useMemo(() => {
    const ids = new Set<string>();
    if (currentId) ids.add(currentId);
    history.forEach((item) => ids.add(item.participantId));
    return ids;
  }, [currentId, history]);

  const selectedId =
    requestedId && knownIds.has(requestedId) ? requestedId : currentId;
  const isHistorySelection = Boolean(
    selectedId && currentId && selectedId !== currentId,
  );

  const detailQuery = useQuery({
    queryKey: queryKeys.practiceDetail(selectedId ?? "none"),
    queryFn: () => api.practiceDetail(selectedId as string),
    enabled: isHistorySelection && Boolean(selectedId),
    retry: false,
  });

  const practice: PracticeCurrent | null = isHistorySelection
    ? (detailQuery.data ?? null)
    : current;

  useEffect(() => {
    if (!practicesQuery.data || !requestedId || knownIds.has(requestedId)) return;
    const params = new URLSearchParams(searchParams.toString());
    params.delete("participantId");
    const query = params.toString();
    router.replace(query ? `${pathname}?${query}` : pathname);
  }, [knownIds, pathname, practicesQuery.data, requestedId, router, searchParams]);

  const options = useMemo<PracticeOption[]>(() => {
    const result: PracticeOption[] = [];
    if (current) {
      result.push({
        participantId: current.participantId,
        practiceName: current.practiceName,
        professionalModule: current.professionalModule,
        status: current.status,
        statusLabel: "В процессе",
        dateStart: current.dateStart,
        dateEnd: current.dateEnd,
        organization: current.organization.name,
        grade: current.assessment?.grade ?? null,
      });
    }
    history.forEach((item: PracticeHistoryItem) => {
      result.push({
        participantId: item.participantId,
        practiceName: item.practiceName,
        professionalModule: item.professionalModule,
        status: item.status,
        statusLabel: item.statusLabel,
        dateStart: item.dateStart,
        dateEnd: item.dateEnd,
        organization: item.organization,
        grade: item.grade,
      });
    });
    return result;
  }, [current, history]);

  const select = useCallback(
    (participantId: string) => {
      const params = new URLSearchParams(searchParams.toString());
      if (participantId === currentId) {
        params.delete("participantId");
      } else {
        params.set("participantId", participantId);
      }
      const query = params.toString();
      router.push(query ? `${pathname}?${query}` : pathname);
    },
    [currentId, pathname, router, searchParams],
  );

  const retry = useCallback(() => {
    void practicesQuery.refetch();
    if (isHistorySelection) void detailQuery.refetch();
  }, [detailQuery, isHistorySelection, practicesQuery]);

  return {
    current,
    history,
    practice,
    selectedId,
    isHistorySelection,
    options,
    practicesQuery,
    detailQuery,
    isPending:
      practicesQuery.isPending ||
      (isHistorySelection && detailQuery.isPending),
    error: practicesQuery.error ?? (isHistorySelection ? detailQuery.error : null),
    select,
    retry,
  };
}
