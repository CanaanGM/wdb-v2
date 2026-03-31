# REST Collections

- Postman collection: `Docs/Rest/workoutlog.postman_collection.json`
- Bruno collection folder: `Docs/Rest/bruno`

## Notes

- Default base URL is `https://localhost:6001`.
- For authenticated endpoints, set `accessToken` after login/register/refresh.
- Postman requests for register/login/refresh include a test script that stores `accessToken` automatically.
- Bruno now includes:
  - `07-user-exercise-stats/03-search-query-get.bru` for GET-based stat search.
  - `09-plans/*` for full plans + enrollment + agenda + execution flow.
  - `09-plans/12-create-bulk-admin.bru` for plan bulk create.
  - `09-plans/13-create-day-exercises-bulk-admin.bru` for bulk plan-day exercises.
  - `10-training-types/*` for training type CRUD + bulk creation.
  - `11-measurements/*` for authenticated measurement CRUD.
  - `04-exercises/*` updated with `trainingTypes` support and `trainingTypeName` filtering.
