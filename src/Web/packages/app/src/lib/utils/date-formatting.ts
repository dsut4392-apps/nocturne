/**
 * Date Formatting Utilities
 * Centralized date formatting functions for consistent display across the application
 */

/**
 * Formats a date string to display date and time
 * @param dateStr - ISO date string or undefined
 * @returns Formatted date and time string, or fallback
 */
export function formatDateTime(dateStr: string | undefined): string {
  if (!dateStr) return "—";
  const date = new Date(dateStr);
  return date.toLocaleDateString() + " " + date.toLocaleTimeString();
}

/**
 * Formats a date string or Date object to locale string
 * @param date - Date object, ISO date string, or undefined
 * @returns Formatted date and time string, or "N/A"
 */
export function formatDate(date: Date | string | undefined): string {
  if (!date) return "N/A";
  return new Date(date).toLocaleString();
}

/**
 * Formats a date string with detailed formatting options
 * @param dateString - ISO date string or undefined
 * @returns Formatted date and time with full details, or "Unknown"
 */
export function formatDateDetailed(dateString: string | undefined): string {
  if (!dateString) return "Unknown";
  try {
    return new Date(dateString).toLocaleDateString(undefined, {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return dateString;
  }
}

/**
 * Formats a date string for use in datetime-local input fields
 * @param dateStr - ISO date string or undefined
 * @returns Date in YYYY-MM-DDTHH:MM format for HTML input
 */
export function formatDateForInput(dateStr: string | undefined): string {
  if (!dateStr) return "";
  const date = new Date(dateStr);
  const year = date.getFullYear();
  const month = (date.getMonth() + 1).toString().padStart(2, "0");
  const day = date.getDate().toString().padStart(2, "0");
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

/**
 * Formats a date string to compact date and time (short month)
 * @param dateStr - ISO date string or undefined
 * @returns Compact formatted date and time, or "—"
 */
export function formatDateTimeCompact(dateStr: string | undefined): string {
  if (!dateStr) return "—";
  const date = new Date(dateStr);
  return date.toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
