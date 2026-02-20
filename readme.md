CodeFlow - A stack overflow clone. 

![alt text](image.png)

# Roadmap

### Core Feature Enhancements
- ✅ **Voting System Completion**
  - ✅ Extend voting to answers
  - ✅ Implement vote scoring logic for answers
  - ✅ Add visual feedback for voted posts
  - ✅ Prevent self-voting

- ✅ **Answer Acceptance**
  - ✅ Add "Accept Answer" functionality for question owners
  - ✅ Visual indicator for accepted answers
  - ✅ Reputation reward for accepted answers
  - ✅ Only one accepted answer per question

- ✅ **Comment System**
  - ✅ Create `Comments` table with FK to Questions/Answers
  - ✅ Implement comment repository
  - ✅ Add comment controller actions
  - ✅ AJAX-based comment posting/editing
  - ✅ Comment voting (if needed)
  - ✅ Character limit validation (e.g., 500 chars)

- ✅ **User Profiles & Reputation**
  - ✅ User profile page showing activity
  - ✅ Reputation calculation system
  - ✅ Badges/Achievements system
  - ✅ User activity history (questions, answers, votes)
  - ✅ Profile editing capabilities, bio, image upload

- ✅ **Code Refactor**
  - ✅ Extention method for Program.cs
  - ✅ Custom Filter for actions
  - ✅ Logging system
  - ✅ Fluent Validation
  - ✅ Custom middleware
  - ✅ Authentication and Autorization

### UI/UX Improvements
- ✅ **Pagination**
  - ✅ Implement server-side pagination for question lists
  - ✅ Add "Load More" or traditional page numbers
  - ✅ Configurable page sizes (20, 50, 100)
  - ✅ Add filtering logic 
  - ✅ Add pagination for sub lists
  
- ✅ **Rich Text Editing**
  - ✅ Integrate Markdown editor (e.g., EasyMDE, SimpleMDE)
  - ✅ Syntax highlighting for code blocks
  - ✅ Preview functionality
  - ✅ XSS protection and input sanitization

- ✅ **Mobile Responsiveness**
  - ✅ Optimize for mobile devices
  - ✅ Touch-friendly voting buttons
  - ✅ Responsive navigation

### Caching Strategy
- ✅ **Redis Implementation**
  - ✅ Cache homepage question list (15-30 minute TTL)
  - ✅ Cache frequently accessed questions
  - ✅ Implement cache invalidation strategies

### Community Features
- ✅ **Moderation Tools**
  - ✅ Flagging system for inappropriate content
  - ✅ Moderator dashboard

- ✅ **Notification System**
  - ✅ Real-time notifications (SignalR)
  - ✅ In-app notification center

### Security Hardening
  - [ ] API rate limiting per user/IP
  - [ ] Content Security Policy (CSP)
  - [ ] X-Frame-Options
  - [ ] X-Content-Type-Options
  - [ ] Strict-Transport-Security

---

BUGS: 

- Updated logging output to file. 
- Seperate the program.cs file.
- Load more button fix.
- Add Lazy loading


