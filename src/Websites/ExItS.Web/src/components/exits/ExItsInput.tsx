import * as React from "react";

import { Input, type InputProps } from "@/components/ui/input";

export type ExItsInputProps = InputProps;

export function ExItsInput({ className, ...props }: ExItsInputProps) {
  return <Input className={className} {...props} />;
}

