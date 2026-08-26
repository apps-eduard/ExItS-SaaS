import { render, screen } from "@testing-library/react";

import { MemoryRouter } from "react-router-dom";

import { describe, expect, it, vi } from "vitest";

import { BackgroundJobsPage } from "@/features/background-jobs/BackgroundJobsPage";



vi.mock("@/hooks/use-authorization", () => ({

  useAuthorization: () => ({

    status: "loaded",

    isPlatformAdministrator: true,

    hasAnyPermission: () => true,

    hasPermission: () => true,

  }),

}));



vi.mock("@/hooks/use-preferences", () => ({

  usePreferences: () => ({

    t: (key: string) => key,

    language: "en-GB",

  }),

}));



vi.mock("@tanstack/react-query", () => ({

  useQuery: () => ({

    isPending: false,

    isError: false,

    isSuccess: true,

    data: { items: [], totalCount: 0, page: 1, pageSize: 20 },

    refetch: vi.fn(),

  }),

}));



describe("BackgroundJobsPage", () => {

  it("renders the background jobs table shell", () => {

    render(

      <MemoryRouter>

        <BackgroundJobsPage />

      </MemoryRouter>,

    );

    expect(screen.getByText("backgroundJobs.message")).toBeInTheDocument();

    expect(screen.getByText("backgroundJobs.table.empty")).toBeInTheDocument();

    expect(screen.queryByText("BACKEND_API_GAP: BACKGROUND_JOB_MONITORING")).not.toBeInTheDocument();

  });

});


