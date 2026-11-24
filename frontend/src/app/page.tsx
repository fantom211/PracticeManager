import { redirect } from "next/navigation";

// The root path is redirected in next.config.ts; keep this fallback out of
// instant-navigation validation because `redirect` intentionally blocks render.
export const instant = false;

export default function HomePage() {
  redirect("/practice");
}
