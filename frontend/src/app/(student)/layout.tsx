import { Suspense, type ReactNode } from "react";
import { AppShell } from "@/components/AppShell";
import { LoadingScreen } from "@/components/ui";

export default function StudentLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="shell">
          <div className="content">
            <LoadingScreen />
          </div>
        </div>
      }
    >
      <AppShell>{children}</AppShell>
    </Suspense>
  );
}
