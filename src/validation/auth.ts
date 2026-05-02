import { z } from 'zod'

export const emailSchema = z.object({
  email: z.email()
})

export const authSchema = z.object({
  email: z.email(),
  age: z.string().length(4),
  password: z.string().min(5).max(20),
  confirmPassword: z.string().min(5).max(20)
}).refine((data) => data.password !== data.confirmPassword, {
  message: "Password don't match",
  path: ["confirmPassword"],
}).refine((data) => isNaN(Number(data.age)), {
  message: 'Age must be a number',
  path: ['age']
})