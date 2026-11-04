import {ApiError, type ApiFailure, type ApiSuccess} from "@/types/api.ts";
import {useUserStore} from "@/stores/auth.ts";

const API_BASE = "/api"

export async function api<T>(
    path: string,
    init: RequestInit = {}
): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");

    if (init.body !== undefined) {
        headers.set("Content-Type", "application/json");
    }
    if (useUserStore.getState().accessToken) {
        headers.set("Authorization", `Bearer ${useUserStore.getState().accessToken}`);
    }

    const response = await fetch(`${API_BASE}${path}`, {
        ...init,
        headers,
    });

    const body = await response.json() as | ApiSuccess<T> | ApiFailure;

    if (!response.ok || !body.isSuccess) {
        const failure = body as ApiFailure;
        throw new ApiError(
            response.status,
            failure.error.code,
            failure.error.identifier,
            failure.error.message
        );
    }

    return (body as ApiSuccess<T>).data;
}