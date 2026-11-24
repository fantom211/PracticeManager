import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { PracticeView } from "./practice-view";

export default function PracticePage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <PracticeView />
    </Suspense>
  );
}
