import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ExitsUpload } from "@/components/exits/ExitsUpload";

describe("ExitsUpload", () => {
  it("renders dropzone empty state and opens file input", async () => {
    const user = userEvent.setup();
    const onSelectFiles = vi.fn();
    render(<ExitsUpload variant="dropzone" onSelectFiles={onSelectFiles} testId="upload" />);
    expect(screen.getByTestId("upload-zone")).toHaveTextContent("Drop or select a cover image");
    expect(screen.getByTestId("upload-action")).toHaveTextContent("Upload");
    const input = screen.getByTestId("upload-input") as HTMLInputElement;
    const file = new File(["x"], "cover.png", { type: "image/png" });
    await user.upload(input, file);
    expect(onSelectFiles).toHaveBeenCalledTimes(1);
    expect(onSelectFiles.mock.calls[0][0][0]).toMatchObject({ name: "cover.png" });
  });

  it("shows filled preview with clear for tile", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    render(
      <ExitsUpload
        variant="tile"
        value={{ id: "1", name: "a.png", previewUrl: "blob:mock" }}
        onClear={onClear}
        testId="tile"
      />,
    );
    expect(screen.getByTestId("tile-preview")).toBeInTheDocument();
    await user.click(screen.getByTestId("tile-clear"));
    expect(onClear).toHaveBeenCalled();
  });

  it("renders compact button variant", () => {
    render(<ExitsUpload variant="button" uploadLabel="Upload file" testId="btn" />);
    expect(screen.getByTestId("btn-trigger")).toHaveTextContent("Upload file");
  });

  it("supports multi file list and delete on button compact", async () => {
    const user = userEvent.setup();
    const onSelectFiles = vi.fn();
    const onRemove = vi.fn();
    const { rerender } = render(
      <ExitsUpload
        variant="button"
        multiple
        values={[
          { id: "a", name: "a.pdf" },
          { id: "b", name: "b.pdf" },
        ]}
        onSelectFiles={onSelectFiles}
        onRemove={onRemove}
        testId="btn"
      />,
    );
    expect(screen.getByTestId("btn-files")).toBeInTheDocument();
    expect(screen.getByTestId("btn-file-a")).toHaveTextContent("a.pdf");
    expect(screen.getByTestId("btn-file-b")).toHaveTextContent("b.pdf");
    await user.click(screen.getByTestId("btn-remove-a"));
    expect(onRemove).toHaveBeenCalledWith("a");

    const input = screen.getByTestId("btn-input") as HTMLInputElement;
    expect(input.multiple).toBe(true);
    const files = [
      new File(["1"], "c.pdf", { type: "application/pdf" }),
      new File(["2"], "d.pdf", { type: "application/pdf" }),
    ];
    fireEvent.change(input, { target: { files } });
    expect(onSelectFiles).toHaveBeenCalled();
    expect(onSelectFiles.mock.calls[0][0]).toHaveLength(2);

    rerender(
      <ExitsUpload
        variant="button"
        multiple
        values={[{ id: "b", name: "b.pdf" }]}
        onRemove={onRemove}
        testId="btn"
      />,
    );
    expect(screen.queryByTestId("btn-file-a")).not.toBeInTheDocument();
    expect(screen.getByTestId("btn-file-b")).toBeInTheDocument();
  });

  it("accepts drag-and-drop on dropzone", () => {
    const onSelectFiles = vi.fn();
    render(<ExitsUpload variant="dropzone" onSelectFiles={onSelectFiles} testId="drop" />);
    const zone = screen.getByTestId("drop-zone");
    const file = new File(["x"], "dragged.jpg", { type: "image/jpeg" });
    fireEvent.drop(zone, {
      dataTransfer: { files: [file] },
    });
    expect(onSelectFiles).toHaveBeenCalled();
    expect(onSelectFiles.mock.calls[0][0][0].name).toBe("dragged.jpg");
  });
});
