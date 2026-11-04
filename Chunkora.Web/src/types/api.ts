export class ApiError extends Error {
    status: number;
    code: number;
    identifier: string;

    constructor(
        status: number,
        code: number,
        identifier: string,
        message: string
    ) {
        super(message);
        this.status = status;
        this.code = code;
        this.identifier = identifier;
    }
}

export interface ApiSuccess<T> {
    isSuccess: true;
    data: T;
}

export interface ApiFailureError {
    code: number;
    identifier: string;
    message: string;
}

export interface ApiFailure {
    isSuccess: false;
    error: ApiFailureError;
}

export interface PageResult<T> {
    offset: number;
    limit: number;
    total: number;
    count: number;
    items: T[];
}