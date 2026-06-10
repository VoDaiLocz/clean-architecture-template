export type ToeicPartId = 1 | 2 | 3 | 4 | 5 | 6 | 7;

export type ToeicSkill = 'Listening' | 'Reading';

export type ToeicPart = {
  id: ToeicPartId;
  title: string;
  shortName: string;
  skill: ToeicSkill;
  durationMinutes: number;
  questionCount: number;
  availableTests: number;
  level: 'Foundation' | 'Skill Builder' | 'Score Builder';
  userOutcome: string;
  studyActions: string[];
  roadmap: string[];
  commonMistakes: string[];
};

export const toeicParts: ToeicPart[] = [
  {
    id: 1,
    title: 'Part 1 - Photographs',
    shortName: 'Mô tả tranh',
    skill: 'Listening',
    durationMinutes: 12,
    questionCount: 6,
    availableTests: 18,
    level: 'Foundation',
    userOutcome: 'Nghe nhanh chủ thể, hành động và bối cảnh trong hình.',
    studyActions: ['Nhận diện chủ thể', 'Lọc bẫy từ đồng âm', 'Làm mini test 6 câu'],
    roadmap: ['Học mẫu câu mô tả người/vật', 'Luyện nghe động từ hành động', 'Làm đề ngắn và sửa lỗi theo tranh'],
    commonMistakes: ['Chọn câu đúng từ khóa nhưng sai hành động', 'Bỏ qua vị trí đồ vật', 'Nghe nhầm thì hiện tại tiếp diễn'],
  },
  {
    id: 2,
    title: 'Part 2 - Question-Response',
    shortName: 'Hỏi đáp ngắn',
    skill: 'Listening',
    durationMinutes: 18,
    questionCount: 25,
    availableTests: 24,
    level: 'Foundation',
    userOutcome: 'Phản xạ với câu hỏi ngắn, loại đáp án lạc chủ đề và bẫy âm.',
    studyActions: ['Drill WH-question', 'Drill Yes/No', 'Luyện phản xạ 25 câu'],
    roadmap: ['Phân loại dạng câu hỏi', 'Học chiến thuật loại đáp án', 'Làm full set và nghe lại câu sai'],
    commonMistakes: ['Đợi nghe từng chữ nên mất nhịp', 'Chọn đáp án lặp lại từ khóa', 'Không nhận ra câu trả lời gián tiếp'],
  },
  {
    id: 3,
    title: 'Part 3 - Conversations',
    shortName: 'Hội thoại',
    skill: 'Listening',
    durationMinutes: 32,
    questionCount: 39,
    availableTests: 20,
    level: 'Skill Builder',
    userOutcome: 'Theo dõi hội thoại, bắt ý chính, chi tiết và suy luận theo ngữ cảnh.',
    studyActions: ['Preview câu hỏi', 'Nghe theo cụm 3 câu', 'Review transcript'],
    roadmap: ['Học cách đọc trước câu hỏi', 'Luyện bắt speaker + mục đích', 'Làm hội thoại dài và phân tích transcript'],
    commonMistakes: ['Không đọc trước câu hỏi', 'Mất dấu người nói', 'Chọn đáp án nghe giống nhưng sai ngữ cảnh'],
  },
  {
    id: 4,
    title: 'Part 4 - Talks',
    shortName: 'Bài nói',
    skill: 'Listening',
    durationMinutes: 30,
    questionCount: 30,
    availableTests: 20,
    level: 'Skill Builder',
    userOutcome: 'Nắm cấu trúc thông báo, tin nhắn, quảng cáo và bài nói công việc.',
    studyActions: ['Nghe ý chính', 'Bắt số liệu/thời gian', 'Review script'],
    roadmap: ['Nhận diện loại bài nói', 'Luyện keyword theo vị trí câu hỏi', 'Làm full talk set và ghi lỗi nghe'],
    commonMistakes: ['Bỏ lỡ thông tin số/ngày', 'Không đoán loại thông báo', 'Mất tập trung ở câu cuối'],
  },
  {
    id: 5,
    title: 'Part 5 - Incomplete Sentences',
    shortName: 'Ngữ pháp nhanh',
    skill: 'Reading',
    durationMinutes: 15,
    questionCount: 30,
    availableTests: 36,
    level: 'Foundation',
    userOutcome: 'Tăng tốc chọn đáp án ngữ pháp/từ vựng trong từng câu đơn.',
    studyActions: ['Làm drill theo chủ điểm', 'Sửa lỗi ngay', 'Ôn lại câu sai'],
    roadmap: ['Chẩn đoán ngữ pháp yếu', 'Luyện theo nhóm từ loại/thì/giới từ', 'Canh tốc độ 30 câu trong 15 phút'],
    commonMistakes: ['Nhìn nghĩa mà bỏ cấu trúc', 'Không xác định từ loại cần điền', 'Dành quá nhiều thời gian cho một câu'],
  },
  {
    id: 6,
    title: 'Part 6 - Text Completion',
    shortName: 'Điền đoạn văn',
    skill: 'Reading',
    durationMinutes: 18,
    questionCount: 16,
    availableTests: 18,
    level: 'Skill Builder',
    userOutcome: 'Đọc mạch văn để chọn từ, câu và liên kết ý trong đoạn.',
    studyActions: ['Đọc câu trước/sau', 'Luyện liên từ', 'Làm passage set'],
    roadmap: ['Học dấu hiệu mạch văn', 'Luyện câu điền vào đoạn', 'Làm full set và review logic đoạn'],
    commonMistakes: ['Chỉ đọc câu chứa chỗ trống', 'Bỏ qua đại từ/liên từ', 'Không kiểm tra ý trước sau'],
  },
  {
    id: 7,
    title: 'Part 7 - Reading Comprehension',
    shortName: 'Đọc hiểu',
    skill: 'Reading',
    durationMinutes: 55,
    questionCount: 54,
    availableTests: 28,
    level: 'Score Builder',
    userOutcome: 'Đọc nhanh email, quảng cáo, chat, bài báo và xử lý câu hỏi nhiều đoạn.',
    studyActions: ['Skim câu hỏi', 'Scan chi tiết', 'Làm double/triple passage'],
    roadmap: ['Luyện single passage', 'Luyện double passage theo câu hỏi', 'Canh thời gian full Part 7'],
    commonMistakes: ['Đọc toàn bộ quá chậm', 'Không phân biệt câu hỏi chi tiết/suy luận', 'Nhầm thông tin giữa nhiều đoạn'],
  },
];

export function getToeicPart(id: number): ToeicPart | undefined {
  return toeicParts.find((part) => part.id === id);
}

export function getRecommendedPartIds(): ToeicPartId[] {
  return [5, 2, 3];
}
