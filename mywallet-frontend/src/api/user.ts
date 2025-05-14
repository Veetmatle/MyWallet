export async function login(usernameOrEmail: string, password: string) {
    const response = await fetch("/api/user/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ usernameOrEmail, password }),
    });

    if (!response.ok) {
        const error = await response.text();
        throw new Error(error);
    }

    return await response.json();
}

export async function register(username: string, email: string, password: string) {
    const response = await fetch("/api/user/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, email, password }),
    });

    if (!response.ok) {
        const error = await response.text();
        throw new Error(error);
    }

    return await response.text(); // np. "Rejestracja zakończona sukcesem."
}
