import { z } from 'zod'

const login = z.object({
  email: z.email(),
  password: z.string().min(5).max(20)
})