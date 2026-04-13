import { useState } from 'react';
import { format, parse } from 'date-fns';
import { Calendar as CalendarIcon, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import { Label } from '@/components/ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';

const HOURS = Array.from({ length: 24 }, (_, i) => i);
const MINUTES = Array.from({ length: 60 }, (_, i) => i);

function pad2(n: number) {
  return n.toString().padStart(2, '0');
}

function parseTimeParts(timeValue: string): { h: number; m: number } {
  const m = timeValue?.match(/^(\d{1,2}):(\d{2})$/);
  if (!m) return { h: 0, m: 0 };
  return { h: Math.min(23, parseInt(m[1], 10)), m: Math.min(59, parseInt(m[2], 10)) };
}

function formatTimeButtonLabel(timeValue: string): string {
  if (!timeValue) return 'Pick time';
  const { h, m } = parseTimeParts(timeValue);
  const d = new Date();
  d.setHours(h, m, 0, 0);
  return format(d, 'h:mm a');
}

export interface ReminderDateTimeFieldsProps {
  dateValue: string;
  timeValue: string;
  onDateChange: (date: string) => void;
  onTimeChange: (time: string) => void;
  /** Prefix for element ids (e.g. "jr" / "ar") */
  idPrefix: string;
  className?: string;
}

export function ReminderDateTimeFields({
  dateValue,
  timeValue,
  onDateChange,
  onTimeChange,
  idPrefix,
  className,
}: ReminderDateTimeFieldsProps) {
  const [dateOpen, setDateOpen] = useState(false);
  const [timeOpen, setTimeOpen] = useState(false);

  const selectedDate =
    dateValue && /^\d{4}-\d{2}-\d{2}$/.test(dateValue)
      ? parse(dateValue, 'yyyy-MM-dd', new Date())
      : undefined;

  const { h: selH, m: selM } = parseTimeParts(timeValue || '00:00');

  return (
    <div className={cn('@container w-full min-w-0', className)}>
      <div className="grid w-full min-w-0 grid-cols-1 gap-3 @md:grid-cols-2 @md:items-start">
        <div className="flex min-w-0 flex-col gap-2">
          <Label htmlFor={`${idPrefix}-date-trigger`}>Date</Label>
          <Popover open={dateOpen} onOpenChange={setDateOpen}>
            <PopoverTrigger asChild>
              <Button
                id={`${idPrefix}-date-trigger`}
                type="button"
                variant="outline"
                className={cn(
                  'h-10 w-full min-w-0 justify-start px-3 text-left font-normal',
                  !dateValue && 'text-muted-foreground'
                )}
              >
                <CalendarIcon className="mr-2 h-4 w-4 shrink-0 opacity-70" />
                <span className="min-w-0 flex-1 truncate text-left">
                  {dateValue && selectedDate && !Number.isNaN(selectedDate.getTime())
                    ? format(selectedDate, 'MMM d, yyyy')
                    : 'Pick a date'}
                </span>
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={selectedDate && !Number.isNaN(selectedDate.getTime()) ? selectedDate : undefined}
                onSelect={(d) => {
                  if (d) {
                    onDateChange(format(d, 'yyyy-MM-dd'));
                    setDateOpen(false);
                  }
                }}
                initialFocus
              />
            </PopoverContent>
          </Popover>
        </div>

        <div className="flex min-w-0 flex-col gap-2">
          <Label htmlFor={`${idPrefix}-time-trigger`}>Time</Label>
          <Popover open={timeOpen} onOpenChange={setTimeOpen}>
            <PopoverTrigger asChild>
              <Button
                id={`${idPrefix}-time-trigger`}
                type="button"
                variant="outline"
                className={cn(
                  'h-10 w-full min-w-0 justify-start px-3 text-left font-normal',
                  !timeValue && 'text-muted-foreground'
                )}
              >
                <Clock className="mr-2 h-4 w-4 shrink-0 opacity-70" />
                <span className="min-w-0 flex-1 truncate text-left">{formatTimeButtonLabel(timeValue)}</span>
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-3" align="start">
              <p className="text-xs text-muted-foreground mb-2">Select hour and minute</p>
              <div className="flex gap-3">
                <div>
                  <div className="text-xs font-medium text-center text-muted-foreground mb-1">Hour</div>
                  <div
                    className="h-52 w-[3.25rem] overflow-y-auto rounded-md border border-border p-1 space-y-0.5"
                    role="listbox"
                    aria-label="Hour"
                  >
                    {HOURS.map((h) => (
                      <button
                        key={h}
                        type="button"
                        role="option"
                        aria-selected={h === selH}
                        className={cn(
                          'w-full rounded-sm py-1.5 text-sm tabular-nums transition-colors',
                          h === selH
                            ? 'bg-primary text-primary-foreground'
                            : 'hover:bg-accent hover:text-accent-foreground'
                        )}
                        onClick={() => onTimeChange(`${pad2(h)}:${pad2(selM)}`)}
                      >
                        {pad2(h)}
                      </button>
                    ))}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-medium text-center text-muted-foreground mb-1">Minute</div>
                  <div
                    className="h-52 w-[3.25rem] overflow-y-auto rounded-md border border-border p-1 space-y-0.5"
                    role="listbox"
                    aria-label="Minute"
                  >
                    {MINUTES.map((m) => (
                      <button
                        key={m}
                        type="button"
                        role="option"
                        aria-selected={m === selM}
                        className={cn(
                          'w-full rounded-sm py-1.5 text-sm tabular-nums transition-colors',
                          m === selM
                            ? 'bg-primary text-primary-foreground'
                            : 'hover:bg-accent hover:text-accent-foreground'
                        )}
                        onClick={() => {
                          onTimeChange(`${pad2(selH)}:${pad2(m)}`);
                          setTimeOpen(false);
                        }}
                      >
                        {pad2(m)}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </PopoverContent>
          </Popover>
        </div>
      </div>
    </div>
  );
}
