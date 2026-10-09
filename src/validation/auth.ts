import { z } from "zod";

// Mirrors the API's rules (server/Novashelf.Api/Accounts/AccountRequests.cs); the API is the
// authority, these give instant feedback.
export const PASSWORD_MIN = 8;
export const PASSWORD_MAX = 128;
export const FIRST_NAME_MAX = 60;

const email = z
  .string()
  .trim()
  .min(1, { error: "Enter your email address." })
  .max(254, { error: "That email address is too long." })
  .pipe(z.email({ error: "Enter a valid email address." }));

export const emailSchema = z.object({ email });

export const signUpSchema = z
  .object({
    email,
    firstName: z.string().trim().max(FIRST_NAME_MAX, { error: `Keep it under ${FIRST_NAME_MAX} characters.` }),
    password: z
      .string()
      .min(PASSWORD_MIN, { error: `Use at least ${PASSWORD_MIN} characters.` })
      .max(PASSWORD_MAX, { error: `Use at most ${PASSWORD_MAX} characters.` }),
    confirmPassword: z.string(),
    ageConfirmed: z.literal(true, { error: "You need to be 13 or older to create an account." }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    error: "The passwords don't match.",
    path: ["confirmPassword"],
    // Zod skips object-level checks when any field already failed; check the match anyway so
    // every problem shows at once instead of one per attempt.
    when: ({ value }) =>
      typeof (value as { password?: unknown })?.password === "string" &&
      typeof (value as { confirmPassword?: unknown })?.confirmPassword === "string",
  });

export const signInSchema = z.object({
  email,
  password: z.string().min(1, { error: "Enter your password." }),
});

/** The first message for each field from a failed safeParse, e.g. { email: "Enter a valid…" }. */
export function fieldErrors(error: z.ZodError): Record<string, string> {
  const errors: Record<string, string> = {};
  for (const issue of error.issues) {
    const field = String(issue.path[0] ?? "");
    if (!(field in errors)) errors[field] = issue.message;
  }
  return errors;
}
