/**
 * Scroll `container` just enough to reveal `element`, without moving any ancestor
 * (unlike `Element.scrollIntoView`, which also scrolls the page vertically).
 */
export function scrollWithinContainer(
  container: HTMLElement,
  element: HTMLElement,
  behavior: ScrollBehavior = "auto",
): void {
  const box = container.getBoundingClientRect();
  const target = element.getBoundingClientRect();

  let left = 0;
  if (target.left < box.left) left = target.left - box.left;
  else if (target.right > box.right) left = target.right - box.right;

  let top = 0;
  if (target.top < box.top) top = target.top - box.top;
  else if (target.bottom > box.bottom) top = target.bottom - box.bottom;

  if (left === 0 && top === 0) return;
  if (typeof container.scrollBy === "function") {
    container.scrollBy({ left, top, behavior });
  } else {
    container.scrollLeft += left;
    container.scrollTop += top;
  }
}
