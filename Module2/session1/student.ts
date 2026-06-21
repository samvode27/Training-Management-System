interface Student {
    readonly id: string;
    name: string;
    gpa?: number;
}

const student: Student = {
    id: "STU-001",
    name: "Hana"
};

console.log(student);

console.log(
    student.gpa?.toFixed(2) ?? "Not Yet Graded"
);