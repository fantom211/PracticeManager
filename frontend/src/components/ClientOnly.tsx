"use client";

import { useSyncExternalStore, type ReactNode } from "react";

const subscribe = () => () => {};
const getClientSnapshot = () => true;
const getServerSnapshot = () => false;

/**
 * Renders stable placeholder markup during SSR and hydration. Client components
 * that read TanStack Query state must not render data before hydration, or the
 * prerendered HTML and the first client render can disagree.
 */
export function ClientOnly({
  children,
  fallback = null,
}: {
  children: ReactNode;
  fallback?: ReactNode;
}) {
  const mounted = useSyncExternalStore(subscribe, getClientSnapshot, getServerSnapshot);
  return <>{mounted ? children : fallback}</>;
}
