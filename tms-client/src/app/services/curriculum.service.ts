import { Injectable } from '@angular/core';

export interface CourseModule {
  week: string;
  title: string;
  focus: string;
  lab: string;
}

export interface CourseCurriculum {
  summary: string;
  description: string;
  department: string;
  prerequisites: string;
  credits: number;
  learningOutcomes: string[];
  modules: CourseModule[];
  industrySkills: string[];
  careerOpportunities: string[];
}

@Injectable({
  providedIn: 'root',
})
export class CurriculumService {
  private readonly storagePrefix = 'tms_curriculum_';

  getCurriculum(courseId: number | string, title: string = '', code: string = '', courseEntity?: any): CourseCurriculum {
    if (courseEntity && (courseEntity.description || courseEntity.summary || courseEntity.syllabusJson)) {
      let outcomes: string[] = [];
      let modules: CourseModule[] = [];
      let skills: string[] = [];

      try {
        if (courseEntity.learningOutcomesJson) outcomes = JSON.parse(courseEntity.learningOutcomesJson);
      } catch (e) {}

      try {
        if (courseEntity.syllabusJson) modules = JSON.parse(courseEntity.syllabusJson);
      } catch (e) {}

      try {
        if (courseEntity.industrySkillsJson) skills = JSON.parse(courseEntity.industrySkillsJson);
      } catch (e) {}

      const fallback = this.generateDefaultCurriculum(title || courseEntity.title, code || courseEntity.code);

      return {
        summary: courseEntity.summary || fallback.summary,
        description: courseEntity.description || fallback.description,
        department: courseEntity.department || fallback.department,
        prerequisites: courseEntity.prerequisites || fallback.prerequisites,
        credits: courseEntity.credits || fallback.credits || 3.0,
        learningOutcomes: outcomes.length > 0 ? outcomes : fallback.learningOutcomes,
        modules: modules.length > 0 ? modules : fallback.modules,
        industrySkills: skills.length > 0 ? skills : fallback.industrySkills,
        careerOpportunities: fallback.careerOpportunities
      };
    }

    const key = `${this.storagePrefix}${courseId}`;
    const saved = localStorage.getItem(key);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch (e) {
        console.error('Failed to parse saved curriculum', e);
      }
    }

    // Try key by code as fallback
    if (code) {
      const codeKey = `${this.storagePrefix}code_${code.toUpperCase().trim()}`;
      const savedByCode = localStorage.getItem(codeKey);
      if (savedByCode) {
        try {
          return JSON.parse(savedByCode);
        } catch (e) {}
      }
    }

    return this.generateDefaultCurriculum(title, code);
  }

  saveCurriculum(courseId: number | string, curriculum: CourseCurriculum, code?: string): void {
    const key = `${this.storagePrefix}${courseId}`;
    localStorage.setItem(key, JSON.stringify(curriculum));
    if (code) {
      const codeKey = `${this.storagePrefix}code_${code.toUpperCase().trim()}`;
      localStorage.setItem(codeKey, JSON.stringify(curriculum));
    }
  }

  generateDefaultCurriculum(title: string, code: string): CourseCurriculum {
    const t = (title || '').toLowerCase();
    const c = (code || '').toUpperCase();

    if (t.includes('cloud') || c.includes('CLD') || c.includes('AWS') || c.includes('INSTRU')) {
      return {
        summary: 'Architecting resilient, elastic cloud microservices, multi-region deployments, and automated infrastructure as code.',
        description:
          'This advanced course explores modern cloud engineering principles, container orchestration with Kubernetes, infrastructure as code using Terraform, and enterprise cloud migration patterns. Students gain hands-on experience designing secure, multi-tier architectures compliant with ISO 27001 and SOC 2 frameworks.',
        department: 'School of Distributed Computing & DevOps',
        prerequisites: 'Foundational understanding of Networking and Linux Operating Systems (CS-201 or equivalent).',
        credits: 3.0,
        learningOutcomes: [
          'Design highly available, fault-tolerant architectures across distributed cloud regions.',
          'Deploy automated CI/CD pipelines incorporating container security and image scanning.',
          'Implement event-driven serverless architectures and message brokers.',
          'Analyze operational telemetry and formulate cost-optimization strategies.'
        ],
        modules: [
          { week: 'Weeks 1–4', title: 'Virtualization & Cloud Infrastructure', focus: 'VPC subnets, routing tables, security groups, and IAM least-privilege policies.', lab: 'Provisioning VPC and Bastion Hosts' },
          { week: 'Weeks 5–8', title: 'Containerization & Orchestration', focus: 'Docker containerization, Kubernetes clusters, ingress controllers, and ConfigMaps.', lab: 'Deploying Microservices on Kubernetes' },
          { week: 'Weeks 9–12', title: 'Infrastructure as Code & CI/CD', focus: 'Terraform state management, GitHub Actions workflows, and automated blue/green rollouts.', lab: 'Terraform Multi-Cloud Automation' },
          { week: 'Weeks 13–16', title: 'Observability & Capstone Defense', focus: 'Prometheus metrics, Grafana dashboards, disaster recovery drills, and capstone presentation.', lab: 'Full Stack Cloud Resilience Benchmark' }
        ],
        industrySkills: ['Terraform', 'Kubernetes', 'AWS Architecture', 'Docker', 'CI/CD Pipelines', 'Prometheus'],
        careerOpportunities: ['Cloud Solutions Architect', 'DevOps Systems Engineer', 'Site Reliability Engineer (SRE)']
      };
    }

    if (t.includes('data') || t.includes('algorithm') || c.includes('CS-101') || c.includes('CS-201') || c.includes('DSA')) {
      return {
        summary: 'Deep-dive into fundamental algorithmic paradigms, computational complexity analysis, and scalable memory management.',
        description:
          'An essential core curriculum module investigating asymptotic complexity, tree balances, graph traversals, and dynamic programming. Students solve rigorous computational challenges, optimizing time and space trade-offs in mission-critical applications.',
        department: 'Department of Computer Science & Software Engineering',
        prerequisites: 'Introductory Programming in C++, C#, or Java (CS-100).',
        credits: 3.0,
        learningOutcomes: [
          'Calculate asymptotic time and space bounds using Big-O, Big-Omega, and Master theorem.',
          'Implement balanced binary search trees, red-black trees, and hash-based structures from scratch.',
          'Formulate optimal greedy, divide-and-conquer, and dynamic programming algorithms.',
          'Solve network routing and pathfinding problems using Dijkstra, A*, and Bellman-Ford.'
        ],
        modules: [
          { week: 'Weeks 1–4', title: 'Asymptotic Analysis & Linear Structures', focus: 'Big-O notation, linked lists, ring buffers, and amortized complexity arrays.', lab: 'Custom High-Performance Memory Allocator' },
          { week: 'Weeks 5–8', title: 'Trees, Heaps & Priority Queues', focus: 'AVL trees, Red-Black balancing, binary heaps, and Huffman encoding.', lab: 'Self-Balancing Index Engine' },
          { week: 'Weeks 9–12', title: 'Graph Algorithms & Traversals', focus: 'BFS, DFS, topological sorting, Minimum Spanning Trees, and max flow algorithms.', lab: 'Real-Time Urban Transit Router' },
          { week: 'Weeks 13–16', title: 'Dynamic Programming & NP-Hard Problems', focus: 'Memoization tables, knapsack problem variants, approximation algorithms, and final examination.', lab: 'Comprehensive Optimization Engine' }
        ],
        industrySkills: ['Algorithm Design', 'Time Complexity Optimization', 'Graph Theory', 'Data Structures', 'C++ / C# Performance'],
        careerOpportunities: ['Core Systems Engineer', 'Quantitative Software Developer', 'Backend Infrastructure Architect']
      };
    }

    if (t.includes('web') || t.includes('frontend') || t.includes('angular') || t.includes('api') || c.includes('CSE') || c.includes('WEB')) {
      return {
        summary: 'Modern full-stack web engineering with reactive state architectures, RESTful APIs, and progressive web applications.',
        description:
          'Master enterprise full-stack development using modern reactive frameworks, state stores, web security best practices (OAuth2/OIDC, CSRF, XSS defenses), and responsive design systems. Students build dynamic, accessible, and high-performance production applications.',
        department: 'Department of Web Science & Interactive Systems',
        prerequisites: 'Web Basics & Programming Principles (CS-101).',
        credits: 3.0,
        learningOutcomes: [
          'Architect modular single-page web applications with signals and unidirectional data flow.',
          'Construct secure REST and WebSocket communication channels with token refresh strategies.',
          'Implement WCAG 2.1 AA accessible, responsive user interfaces across all viewports.',
          'Integrate automated end-to-end and component unit tests.'
        ],
        modules: [
          { week: 'Weeks 1–4', title: 'Modern Reactive Architecture', focus: 'Signals, component lifecycles, change detection strategies, and dependency injection.', lab: 'Reactive Interactive Dashboard' },
          { week: 'Weeks 5–8', title: 'State Management & Async Streams', focus: 'Signals store, RxJS pipelines, debouncing, and optimistic UI updates.', lab: 'Real-Time Collaboration Workspace' },
          { week: 'Weeks 9–12', title: 'Web Security & Real-Time SignalR', focus: 'JWT interceptors, role guards, WebSocket push streaming, and token management.', lab: 'Secure Authentication & Hub Engine' },
          { week: 'Weeks 13–16', title: 'Production Optimization & Deployment', focus: 'Bundle tree-shaking, lazy loading, SSR hydration, and capstone presentation.', lab: 'Enterprise Portal Capstone Launch' }
        ],
        industrySkills: ['Angular / TypeScript', 'SignalR WebSockets', 'Responsive SCSS', 'REST APIs', 'Web Security (JWT/XSS)'],
        careerOpportunities: ['Full-Stack Web Engineer', 'Frontend Application Architect', 'UI/UX Systems Developer']
      };
    }

    return {
      summary: `Comprehensive theoretical and applied curriculum covering industry standards, architectural design, and evaluation in ${title || 'Academic Course'}.`,
      description: `This university-accredited module ${code || 'TMS'} (${title || 'Course'}) delivers rigorous instructional coursework paired with interactive laboratory milestones. Students explore core domain principles, collaborate on group deliverables, and gain practical expertise aligned with modern industrial practices.`,
      department: 'Faculty of Information Technology & Engineering',
      prerequisites: 'Standard departmental admission and prerequisite clearance.',
      credits: 3.0,
      learningOutcomes: [
        `Master foundational methodologies and core engineering principles in ${title || 'this domain'}.`,
        'Formulate quantitative technical evaluations and automated verification tests.',
        'Collaborate using enterprise workflows, version control, and code reviews.',
        'Deliver a comprehensive semester capstone portfolio demonstrating subject mastery.'
      ],
      modules: [
        { week: 'Weeks 1–4', title: 'Domain Foundations & Baseline Setups', focus: 'Core paradigms, environment initialization, and theoretical overview.', lab: 'Initial Benchmark Lab' },
        { week: 'Weeks 5–8', title: 'Intermediate Applications & Algorithms', focus: 'Applied techniques, concurrency, and modular architecture.', lab: 'Interactive Component Milestone' },
        { week: 'Weeks 9–12', title: 'Security, Integration & Compliance', focus: 'Role-based access, error mitigation, and system integration.', lab: 'Enterprise Verification Suite' },
        { week: 'Weeks 13–16', title: 'Capstone Implementation & Final Defense', focus: 'Comprehensive evaluation, final score assessment, and certification.', lab: 'Capstone Defense & Release' }
      ],
      industrySkills: ['Technical Architecture', 'Domain Problem Solving', 'System Design', 'Quality Assurance'],
      careerOpportunities: [`Specialist in ${title || 'Domain'}`, 'Technical Consultant', 'Software Engineer']
    };
  }
}
