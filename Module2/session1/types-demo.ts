let unsafeData: any = "85";

const apiResponse: unknown =
{
    id: "STU-001",
    name: "Hana"
};

if (
    typeof apiResponse === "object" &&
    apiResponse !== null
)
{
    console.log("Validated");
}

function assertNever(value: never): never
{
    throw new Error(`Unexpected value: ${value}`);
}