import { Suspense } from "react";
import { LoadingScreen } from "@/components/ui";
import { TasksView } from "./tasks-view";

export default function TasksPage() {
  return (
    <Suspense fallback={<LoadingScreen />}>
      <TasksView />
    </Suspense>
  );
}
