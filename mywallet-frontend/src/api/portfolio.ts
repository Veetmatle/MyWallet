export async function createPortfolio(userId: number, name: string, description?: string) {
    const response = await fetch("/api/portfolio", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            name,
            description,
            userId, // to dodamy do dto w kolejnym kroku, jeśli jeszcze go tam nie ma
            createdAt: new Date().toISOString(),
        }),
    });

    if (!response.ok) {
        throw new Error("Nie udało się dodać portfela.");
    }

    return response.json();
}
