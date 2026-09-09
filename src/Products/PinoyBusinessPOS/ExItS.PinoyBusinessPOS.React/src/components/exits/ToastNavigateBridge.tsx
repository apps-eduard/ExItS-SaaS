import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { setToastNavigate } from "@/components/exits/toast-navigation";

/**
 * Mount under RouterProvider so toast action links can SPA-navigate even though
 * ToastProvider (and its toast region) sit outside the router tree.
 */
export function ToastNavigateBridge() {
  const navigate = useNavigate();

  useEffect(() => {
    setToastNavigate((to) => {
      void navigate(to);
    });
    return () => setToastNavigate(null);
  }, [navigate]);

  return null;
}
