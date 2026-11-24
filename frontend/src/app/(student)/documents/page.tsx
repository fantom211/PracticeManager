import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { DocumentsView } from "./documents-view";

export default function DocumentsPage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <DocumentsView />
    </Suspense>
  );
}
