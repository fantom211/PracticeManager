import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { TemplatesView } from "./templates-view";

export default function TemplatesPage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <TemplatesView />
    </Suspense>
  );
}
