import { Inbox, Info } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { MotionReveal } from "@/motion/MotionReveal";
import { listItemVariants, motionDuration, springSnappy, tweenPage } from "@/motion/presets";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { SearchField } from "@/components/ui/search-field";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { formatCurrency } from "@/lib/format";

const palette = [
  { key: "primary", swatch: "bg-primary", hex: "#0E7885" },
  { key: "secondary", swatch: "bg-secondary", hex: "#2563EB" },
  { key: "personal", swatch: "bg-personal", hex: "#7C3AED" },
  { key: "success", swatch: "bg-success", hex: "#22C55E" },
  { key: "warning", swatch: "bg-warning", hex: "#F59E0B" },
  { key: "error", swatch: "bg-destructive", hex: "#EF4444" },
  { key: "information", swatch: "bg-information", hex: "#2563EB" },
  { key: "background", swatch: "bg-background border", hex: "#F8FAFC" },
  { key: "surface", swatch: "bg-card border", hex: "#FFFFFF" },
  { key: "muted", swatch: "bg-muted", hex: "#F1F5F9" },
  { key: "border", swatch: "bg-border", hex: "#E2E8F0" },
] as const;

function Section({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="space-y-4">
      <h2 className="text-lg font-semibold tracking-tight text-foreground">
        {title}
      </h2>
      {children}
    </section>
  );
}

function DesignSystemPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const [checked, setChecked] = useState(true);
  const [enabled, setEnabled] = useState(true);
  const [motionItems, setMotionItems] = useState(["A", "B", "C"]);
  const reduced = useReducedMotion();

  return (
    <div className="space-y-10">
      <header className="space-y-2">
        <h1 className="text-2xl font-bold tracking-tight md:text-3xl">
          {t("designSystem.title")}
        </h1>
        <p className="max-w-2xl text-sm text-muted-foreground md:text-base">
          {t("designSystem.subtitle")}
        </p>
      </header>

      <Section title={t("designSystem.colors")}>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {palette.map((color) => (
            <div
              key={color.key}
              className="overflow-hidden rounded-xl border border-border bg-card shadow-sm"
            >
              <div className={`h-16 ${color.swatch}`} />
              <div className="space-y-0.5 p-3">
                <p className="text-sm font-semibold">
                  {t(`designSystem.${color.key}`)}
                </p>
                <p className="font-mono text-xs text-muted-foreground">
                  {color.hex}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Section>

      <Section title={t("designSystem.typography")}>
        <Card>
          <CardContent className="space-y-4 p-5">
            <p className="text-3xl font-bold tracking-tight">
              {t("designSystem.pageTitle")}
            </p>
            <p className="text-xl font-semibold">
              {t("designSystem.heading")}
            </p>
            <p className="text-base text-foreground">
              {t("designSystem.body")}
            </p>
            <p className="text-sm font-medium">{t("designSystem.label")}</p>
            <p className="text-xs text-muted-foreground">
              {t("designSystem.caption")}
            </p>
            <p className="font-mono text-2xl font-semibold tabular-nums">
              {t("designSystem.numeric")}
            </p>
          </CardContent>
        </Card>
      </Section>

      <Section title={t("designSystem.buttons")}>
        <Card>
          <CardContent className="flex flex-wrap items-center gap-3 p-5">
            <Button variant="primary">{t("common.save")}</Button>
            <Button variant="secondary">{t("common.continue")}</Button>
            <Button variant="outline">{t("common.cancel")}</Button>
            <Button variant="ghost">{t("common.close")}</Button>
            <Button variant="destructive">{t("common.cancel")}</Button>
            <Button disabled>{t("common.disabled")}</Button>
            <Button loading>{t("common.loading")}</Button>
            <Button size="sm">SM</Button>
            <Button size="md">MD</Button>
            <Button size="lg">LG</Button>
          </CardContent>
        </Card>
      </Section>

      <Section title={t("designSystem.inputs")}>
        <Card>
          <CardContent className="grid gap-5 p-5 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="ds-text">{t("designSystem.label")}</Label>
              <Input id="ds-text" placeholder={t("designSystem.label")} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ds-search">{t("common.search")}</Label>
              <SearchField
                id="ds-search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("designSystem.sampleSearch")}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ds-password">
                {t("designSystem.samplePassword")}
              </Label>
              <PasswordInput
                id="ds-password"
                placeholder={t("designSystem.samplePassword")}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ds-number">
                {t("designSystem.sampleNumber")}
              </Label>
              <Input
                id="ds-number"
                type="number"
                inputMode="decimal"
                defaultValue={12}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ds-currency">
                {t("designSystem.sampleCurrency")}
              </Label>
              <Input
                id="ds-currency"
                inputMode="decimal"
                defaultValue={formatCurrency(2500).replace(/[^\d.,]/g, "")}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ds-date">{t("designSystem.sampleDate")}</Label>
              <Input id="ds-date" type="date" />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="ds-notes">
                {t("designSystem.sampleTextarea")}
              </Label>
              <Textarea
                id="ds-notes"
                placeholder={t("designSystem.sampleTextarea")}
              />
            </div>
            <div className="space-y-2">
              <Label>{t("designSystem.sampleSelect")}</Label>
              <Select defaultValue="main">
                <SelectTrigger aria-label={t("designSystem.sampleSelect")}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="main">{t("common.optionA")}</SelectItem>
                  <SelectItem value="downtown">
                    {t("common.optionB")}
                  </SelectItem>
                  <SelectItem value="online">{t("common.optionC")}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-4">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="ds-check"
                  checked={checked}
                  onCheckedChange={(v) => setChecked(v === true)}
                />
                <Label htmlFor="ds-check">{t("common.enabled")}</Label>
              </div>
              <RadioGroup defaultValue="a" className="flex gap-4">
                <div className="flex items-center gap-2">
                  <RadioGroupItem value="a" id="r-a" />
                  <Label htmlFor="r-a">{t("common.optionA")}</Label>
                </div>
                <div className="flex items-center gap-2">
                  <RadioGroupItem value="b" id="r-b" />
                  <Label htmlFor="r-b">{t("common.optionB")}</Label>
                </div>
              </RadioGroup>
              <div className="flex items-center gap-2">
                <Switch
                  id="ds-switch"
                  checked={enabled}
                  onCheckedChange={setEnabled}
                />
                <Label htmlFor="ds-switch">
                  {enabled ? t("common.enabled") : t("common.disabled")}
                </Label>
              </div>
            </div>
          </CardContent>
        </Card>
      </Section>

      <Section title={t("designSystem.components")}>
        <div className="grid gap-4">
          <Card>
            <CardHeader>
              <CardTitle>Cards · Badges · Tabs</CardTitle>
              <CardDescription>
                Reusable surfaces for product content.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex flex-wrap gap-2">
                <Badge>{t("designSystem.badgeActive")}</Badge>
                <Badge variant="warning">
                  {t("designSystem.badgePending")}
                </Badge>
                <Badge variant="success">OK</Badge>
                <Badge variant="personal">Personal</Badge>
              </div>
              <Tabs defaultValue="one">
                <TabsList>
                  <TabsTrigger value="one">Overview</TabsTrigger>
                  <TabsTrigger value="two">Details</TabsTrigger>
                </TabsList>
                <TabsContent value="one">
                  <p className="text-sm text-muted-foreground">
                    {t("designSystem.body")}
                  </p>
                </TabsContent>
                <TabsContent value="two">
                  <p className="text-sm text-muted-foreground">
                    {t("designSystem.caption")}
                  </p>
                </TabsContent>
              </Tabs>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Table</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t("designSystem.tableName")}</TableHead>
                    <TableHead>{t("designSystem.tableStatus")}</TableHead>
                    <TableHead className="text-right">
                      {t("designSystem.tableAmount")}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow>
                    <TableCell>Maria Cruz</TableCell>
                    <TableCell>
                      <Badge variant="success">
                        {t("designSystem.badgeActive")}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right font-mono tabular-nums">
                      {formatCurrency(1250)}
                    </TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell>Juan Dela Cruz</TableCell>
                    <TableCell>
                      <Badge variant="warning">
                        {t("designSystem.badgePending")}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right font-mono tabular-nums">
                      {formatCurrency(4800)}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          <div className="flex flex-wrap gap-3">
            <Dialog>
              <DialogTrigger asChild>
                <Button variant="outline">
                  {t("designSystem.openDialog")}
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>{t("designSystem.dialogTitle")}</DialogTitle>
                  <DialogDescription>
                    {t("designSystem.dialogBody")}
                  </DialogDescription>
                </DialogHeader>
                <DialogFooter>
                  <Button variant="outline">{t("common.cancel")}</Button>
                  <Button>{t("common.save")}</Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>

            <Sheet>
              <SheetTrigger asChild>
                <Button variant="outline">{t("designSystem.openSheet")}</Button>
              </SheetTrigger>
              <SheetContent>
                <SheetHeader>
                  <SheetTitle>{t("designSystem.sheetTitle")}</SheetTitle>
                  <SheetDescription>
                    {t("designSystem.sheetBody")}
                  </SheetDescription>
                </SheetHeader>
              </SheetContent>
            </Sheet>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline">Dropdown</Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuItem>{t("nav.profile")}</DropdownMenuItem>
                <DropdownMenuItem>{t("nav.settings")}</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost" size="icon" aria-label="Info">
                  <Info className="size-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>{t("designSystem.tooltipHint")}</TooltipContent>
            </Tooltip>

            <Button
              variant="secondary"
              onClick={() => toast.success(t("designSystem.toastSuccess"))}
            >
              {t("designSystem.showToast")}
            </Button>
          </div>

          <Alert variant="information">
            <Info />
            <AlertTitle>{t("designSystem.alertTitle")}</AlertTitle>
            <AlertDescription>{t("designSystem.alertBody")}</AlertDescription>
          </Alert>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-3 rounded-xl border border-border p-4">
              <p className="text-sm font-medium">{t("designSystem.loading")}</p>
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
              <Spinner label={t("common.loading")} />
            </div>
            <EmptyState
              icon={<Inbox />}
              title={t("designSystem.emptyTitle")}
              description={t("designSystem.emptyBody")}
            />
          </div>
        </div>
      </Section>

      <Section title={t("designSystem.motion")}>
        <Card>
          <CardHeader>
            <CardTitle>{t("designSystem.motionTitle")}</CardTitle>
            <CardDescription>{t("designSystem.motionBody")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <p className="text-sm font-medium">{t("designSystem.motionPress")}</p>
              <div className="flex flex-wrap gap-2">
                <Button>{t("common.save")}</Button>
                <Button variant="outline">{t("common.cancel")}</Button>
                <Button disabled>{t("common.disabled")}</Button>
              </div>
            </div>

            <div className="space-y-2">
              <p className="text-sm font-medium">{t("designSystem.motionReveal")}</p>
              <div className="grid gap-2 sm:grid-cols-3">
                {[0, 1, 2].map((index) => (
                  <MotionReveal key={index} index={index}>
                    <div className="rounded-xl border border-border bg-muted/40 p-4 text-sm font-medium">
                      {t("designSystem.motionCard")} {index + 1}
                    </div>
                  </MotionReveal>
                ))}
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex flex-wrap items-center gap-2">
                <p className="text-sm font-medium">{t("designSystem.motionList")}</p>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() =>
                    setMotionItems((items) => [
                      ...items,
                      String.fromCharCode(65 + items.length),
                    ])
                  }
                >
                  {t("designSystem.motionAdd")}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() =>
                    setMotionItems((items) => items.slice(0, -1))
                  }
                  disabled={motionItems.length === 0}
                >
                  {t("designSystem.motionRemove")}
                </Button>
              </div>
              <ul className="divide-y divide-border rounded-xl border border-border">
                <AnimatePresence initial={false}>
                  {motionItems.map((item, index) => (
                    <motion.li
                      key={item}
                      layout={!reduced}
                      variants={reduced ? undefined : listItemVariants}
                      initial={reduced ? false : "hidden"}
                      animate="visible"
                      exit={reduced ? undefined : "exit"}
                      transition={{
                        ...tweenPage,
                        duration: motionDuration.list,
                        delay: Math.min(index * 0.03, 0.12),
                      }}
                      className="px-4 py-3 text-sm"
                    >
                      {t("designSystem.motionItem")} {item}
                    </motion.li>
                  ))}
                </AnimatePresence>
              </ul>
            </div>

            <div className="space-y-2">
              <p className="text-sm font-medium">{t("designSystem.motionSurfaces")}</p>
              <p className="text-xs text-muted-foreground">
                {t("designSystem.motionSurfacesHint")}
              </p>
              {!reduced ? (
                <motion.div
                  className="h-2 overflow-hidden rounded-full bg-muted"
                  initial={false}
                >
                  <motion.div
                    className="h-full w-1/2 rounded-full bg-primary"
                    layout
                    transition={springSnappy}
                  />
                </motion.div>
              ) : (
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div className="h-full w-1/2 rounded-full bg-primary" />
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </Section>
    </div>
  );
}

export { DesignSystemPage };
