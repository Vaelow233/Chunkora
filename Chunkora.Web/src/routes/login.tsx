import {createFileRoute, redirect, useRouter} from "@tanstack/react-router"

import { useUserStore } from "@/stores/auth"
import {Field, FieldGroup, FieldLabel, FieldSet} from "@/components/ui/field.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Button} from "@/components/ui/button.tsx";
import {useState} from "react";
import {toast} from "@/components/ui/toast.tsx";
import {login} from "@/api/user.ts";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {getSelfRole} from "@/api/role.ts";

export const Route = createFileRoute("/login")({
    validateSearch: (search) => ({
        redirect:
            typeof search.redirect === "string"
                ? search.redirect
                : "/",
    }),

    beforeLoad: ({ search }) => {
        if (useUserStore.getState().accessToken) {
            throw redirect({
                to: search.redirect,
            })
        }
    },

    component: LoginPage,
})

function LoginPage() {
    const [name, setName] = useState("");
    const [password, setPassword] = useState("")
    const [loading, setLoading] = useState<boolean>(false)

    const store = useUserStore()
    const router = useRouter()

    async function handleLogin() {
        setLoading(true)

        try {
            const user = await login({name, password});
            store.login(user)
            const role = await getSelfRole()
            store.updateRole(role)
            toast.add({
                type: "success",
                title: "Login successfully.",
            })
            await router.navigate({ to: "/", replace: true })
        } catch (e) {
            toast.add({
                type: "error",
                title: "Failed to login.",
                description: (e instanceof Error ? e.message : String(e)),
            })
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="flex items-center justify-center h-screen">
            <Card className="w-96">
                <CardHeader>
                    <CardTitle>
                        <span className="text-xl font-bold">Login to Chunkora</span>
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <FieldSet>
                        <FieldGroup>
                            <Field>
                                <FieldLabel>Username</FieldLabel>
                                <Input value={name}
                                       onChange={(event) => setName(event.target.value)}
                                       id="username"
                                       autoComplete="off"
                                />
                            </Field>
                            <Field>
                                <FieldLabel>Password</FieldLabel>
                                <Input value={password}
                                       onChange={(event) => setPassword(event.target.value)}
                                       type="password"
                                       id="password"
                                       autoComplete="off"
                                />
                            </Field>
                            <Field>
                                <Button type="submit" onClick={handleLogin} disabled={loading}>Login</Button>
                            </Field>
                        </FieldGroup>
                    </FieldSet>
                </CardContent>
            </Card>
        </div>
    )
}