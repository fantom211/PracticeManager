import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { FillView } from "./fill-view";

export default function FillDocumentPage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <FillView />
    </Suspense>
  );
}
