import { z } from 'zod'

export const emailSchema = z.object({
  email: z.email()
})

export const authSchema = z.object({
  email: z.email(),
  age: z.number().max(4),
  password: z.string().min(5).max(20)
})