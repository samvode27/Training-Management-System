// // Bad
// interface EnrollmentBad
// {
//     isPending:boolean;
//     isApproved:boolean;
//     isActive:boolean;
//     isCompleted:boolean;
//     isDropped:boolean;
// }

// // problem
// {
//    isPending:true,
//    isDropped:true
// }

// // Correct
// type EnrollmentStatus =
//   | { status:"PENDING" }
//   | { status:"APPROVED" }
//   | { status:"ACTIVE" }
//   | { status:"COMPLETED" }
//   | { status:"DROPPED" };


// // Example
// type EnrollmentStatus =
//  | {
//       status:"PENDING";
//       studentId:string;
//    }
//  | {
//       status:"ACTIVE";
//       currentGrade?:number;
//    }
//  | {
//       status:"COMPLETED";
//       finalGrade:number;
//    };  

// // Switch Statement
// function describe(status:EnrollmentStatus)
// {
//     switch(status.status)
//     {
//         case "PENDING":
//             return "Waiting Approval";

//         case "ACTIVE":
//             return "Currently Learning";

//         case "COMPLETED":
//             return "Course Finished";
//     }
// }

