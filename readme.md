## CodeFlow - A stackoverflow like QA form. 

URL: [Codeflow](https://codeflow-qnv5.onrender.com/)

> Note: Since the application is hosted for free it might take some time to load initially, please wait 1-2 min if that happens.  

#### Screens: 

<img width="1888" height="979" alt="image" src="https://github.com/user-attachments/assets/1a02d86b-8ba5-4744-9155-72c3b65f3a44" />

<img width="1135" height="987" alt="image" src="https://github.com/user-attachments/assets/8ff89ad1-0415-4542-9395-d07e03299500" />

-- 
#### Introduction: 

I created this project while going through the book "Asp.net Core in Action" by Andrew Lock (highly recommended). I build this project by adhering to all the techniques mentioned in the book. I also further challenged myself by not using 'React' (go to preference) and instead using only 'Razor views' with JavaScript. I also only used 'dapper' instead of 'entity' for the same reason.

#### Summery: 

This application is build by utilizing both asp.net razor views and webapi's. It uses cookie based auth services, implemented using Identity, simple but scalable postgresql database connected using dapper, simple caching system using Redis and Redis cloud, 
gRPC Realtime notification system using SignalR Core and some custom filers, middleware’s and background services. 

Features: 

1. QA form with markdown preview support and comment system
2. AJAX responsiveness
3. Scalable postgresql database 
4. Redis for caching hot questions
5. Profile system and user activity tracking
6. Realtime Notification system 
7. Flagging system with moderation dashboard
8. Identity for authentication and authorization
9. Role based authentication
10. Resource authorization
11. Custom background jobs
12. Custom filters and middleware’s
13. Extensive logging system and exception handling
14. Mobile view and dark theme supported

---

# Implementation Roadmap

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


