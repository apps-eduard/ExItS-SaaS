import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import {
  localDateTimeToUtcIso,
  utcIsoToLocalDateTimeInput,
} from "@/api/platform/personal-todo-client";

export type TodoFormState = {
  title: string;
  notes: string;
  dueAtLocal: string;
  reminderAtLocal: string;
  priority: string;
  relatedEntityType: string;
  relatedEntityId: string;
};

export const TODO_PRIORITIES = ["None", "Low", "Normal", "High"] as const;

export const TODO_RELATED_TYPES = [
  { value: "", labelKey: "personal.todo.relatedNone" as const },
  { value: "PersonalUtangRelationship", labelKey: "personal.todo.relatedUtang" as const },
  { value: "PersonalContact", labelKey: "personal.todo.relatedContact" as const },
  { value: "CustomerOrder", labelKey: "personal.todo.relatedOrder" as const },
  { value: "Organization", labelKey: "personal.todo.relatedOrg" as const },
];

export function emptyTodoForm(): TodoFormState {
  return {
    title: "",
    notes: "",
    dueAtLocal: "",
    reminderAtLocal: "",
    priority: "Normal",
    relatedEntityType: "",
    relatedEntityId: "",
  };
}

export function todoFormFromDto(todo: PersonalTodoDto): TodoFormState {
  return {
    title: todo.title,
    notes: todo.notes ?? "",
    dueAtLocal: utcIsoToLocalDateTimeInput(todo.dueAtUtc),
    reminderAtLocal: utcIsoToLocalDateTimeInput(todo.reminderAtUtc),
    priority: todo.priority || "None",
    relatedEntityType: todo.relatedEntityType ?? "",
    relatedEntityId: todo.relatedEntityId ?? "",
  };
}

export function todoFormToRequestBody(form: TodoFormState) {
  const relatedType = form.relatedEntityType.trim() || null;
  const relatedId = form.relatedEntityId.trim() || null;
  return {
    title: form.title.trim(),
    notes: form.notes.trim() || null,
    dueAtUtc: localDateTimeToUtcIso(form.dueAtLocal),
    reminderAtUtc: localDateTimeToUtcIso(form.reminderAtLocal),
    priority: form.priority || "None",
    relatedEntityType: relatedType,
    relatedEntityId: relatedType ? relatedId : null,
  };
}

export function applyTodoDeepLinkPrefill(
  form: TodoFormState,
  relatedType: string | null,
  relatedId: string | null,
): TodoFormState {
  if (!relatedType?.trim() || !relatedId?.trim()) {
    return form;
  }
  return {
    ...form,
    relatedEntityType: relatedType.trim(),
    relatedEntityId: relatedId.trim(),
  };
}
