import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { DocumentDetailView } from "./document-detail-view";

export default function DocumentDetailPage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <DocumentDetailView />
    </Suspense>
  );
}
