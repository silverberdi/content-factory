# W06 — Jobs, Budgets, Reliability, and Notifications User Stories


## US-W06-S01-001 — Persist canonical job and step state in PostgreSQL

**Slice:** `W06-S01 — Persistent Job Orchestration`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S01-persistent-job-orchestration`

As an authorized Content Factory user, I want to persist canonical job and step state in PostgreSQL, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S01-002 — Use n8n polling without long sleeping executions

**Slice:** `W06-S01 — Persistent Job Orchestration`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S01-persistent-job-orchestration`

As an authorized Content Factory user, I want to use n8n polling without long sleeping executions, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S01-003 — Maintain idempotency across retries and resumptions

**Slice:** `W06-S01 — Persistent Job Orchestration`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S01-persistent-job-orchestration`

As an authorized Content Factory user, I want to maintain idempotency across retries and resumptions, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S02-001 — Configure monthly budgets by line and channel

**Slice:** `W06-S02 — Budgets and Cost Control`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S02-budgets-and-cost-control`

As an authorized Content Factory user, I want to configure monthly budgets by line and channel, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S02-002 — Track estimated and actual cost by item, provider, and operation

**Slice:** `W06-S02 — Budgets and Cost Control`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S02-budgets-and-cost-control`

As an authorized Content Factory user, I want to track estimated and actual cost by item, provider, and operation, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S02-003 — Warn at configured thresholds and hard-block new paid work at the limit

**Slice:** `W06-S02 — Budgets and Cost Control`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S02-budgets-and-cost-control`

As an authorized Content Factory user, I want to warn at configured thresholds and hard-block new paid work at the limit, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S02-004 — Allow existing paid in-flight work to finish safely

**Slice:** `W06-S02 — Budgets and Cost Control`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S02-budgets-and-cost-control`

As an authorized Content Factory user, I want to allow existing paid in-flight work to finish safely, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S03-001 — Configure bounded retries with safe ranges

**Slice:** `W06-S03 — Retry and Failure Handling`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S03-retry-and-failure-handling`

As an authorized Content Factory user, I want to configure bounded retries with safe ranges, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S03-002 — Use exponential backoff and retryable versus terminal classification

**Slice:** `W06-S03 — Retry and Failure Handling`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S03-retry-and-failure-handling`

As an authorized Content Factory user, I want to use exponential backoff and retryable versus terminal classification, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S03-003 — Escalate exhausted jobs for intervention

**Slice:** `W06-S03 — Retry and Failure Handling`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `CHG-W06-S03-retry-and-failure-handling`

As an authorized Content Factory user, I want to escalate exhausted jobs for intervention, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S04-001 — Receive internal and PWA push notifications without email

**Slice:** `W06-S04 — Notification Center and Web Push`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S04-notification-center-and-web-push`

As an authorized Content Factory user, I want to receive internal and PWA push notifications without email, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S04-002 — Configure notification preferences by user and category

**Slice:** `W06-S04 — Notification Center and Web Push`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S04-notification-center-and-web-push`

As an authorized Content Factory user, I want to configure notification preferences by user and category, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## US-W06-S04-003 — Separate technical and editorial notification categories

**Slice:** `W06-S04 — Notification Center and Web Push`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `CHG-W06-S04-notification-center-and-web-push`

As an authorized Content Factory user, I want to separate technical and editorial notification categories, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.
