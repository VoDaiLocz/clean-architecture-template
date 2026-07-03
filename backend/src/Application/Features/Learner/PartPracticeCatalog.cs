namespace Application.Features.Learner;

public sealed class PartPracticeCatalog
{
    private readonly IReadOnlyDictionary<int, IReadOnlyList<PartPracticeItemResponse>> itemsByPart =
        new Dictionary<int, IReadOnlyList<PartPracticeItemResponse>>
        {
            [1] =
            [
                Item(
                    "p1-photo-001",
                    1,
                    "Listening",
                    "A man is adjusting a projector in a conference room.",
                    "Chọn câu mô tả đúng nhất cho bức tranh.",
                    "A",
                    "Part 1 cần khớp chủ thể và hành động. A đúng vì người đàn ông đang chỉnh máy chiếu.",
                    ("A", "A man is adjusting a piece of equipment."),
                    ("B", "A woman is printing several documents."),
                    ("C", "The chairs are being moved outside."),
                    ("D", "The projector has been turned off.")
                ),
                Item(
                    "p1-photo-002",
                    1,
                    "Listening",
                    "Several boxes are stacked beside a delivery truck.",
                    "Nghe mô tả và loại đáp án sai vị trí hoặc sai hành động.",
                    "C",
                    "C đúng vì các thùng hàng được xếp cạnh xe giao hàng.",
                    ("A", "The truck is parked inside a garage."),
                    ("B", "A worker is opening a cash register."),
                    ("C", "Some boxes are stacked near a truck."),
                    ("D", "The boxes are floating on water.")
                ),
            ],
            [2] =
            [
                Item(
                    "p2-response-001",
                    2,
                    "Listening",
                    "When will the quarterly report be ready?",
                    "Chọn câu trả lời tự nhiên nhất cho câu hỏi WH.",
                    "B",
                    "Câu hỏi hỏi thời điểm. B trả lời bằng mốc thời gian.",
                    ("A", "In the accounting department."),
                    ("B", "By Friday afternoon."),
                    ("C", "It was very detailed."),
                    ("D", "Because the printer is new.")
                ),
                Item(
                    "p2-response-002",
                    2,
                    "Listening",
                    "Would you like me to reserve a meeting room?",
                    "Tránh chọn đáp án lặp từ khóa nhưng không trả lời ý hỏi.",
                    "A",
                    "A là phản hồi phù hợp cho lời đề nghị.",
                    ("A", "Yes, that would be helpful."),
                    ("B", "The meeting lasted one hour."),
                    ("C", "Room 401 is on the fourth floor."),
                    ("D", "I read the reservation policy.")
                ),
            ],
            [3] =
            [
                Item(
                    "p3-conversation-001",
                    3,
                    "Listening",
                    "Two coworkers discuss moving a client call because the manager is delayed.",
                    "Xác định lý do cuộc gọi bị đổi lịch.",
                    "D",
                    "D đúng vì manager bị trễ nên cuộc gọi cần dời lại.",
                    ("A", "The client canceled the contract."),
                    ("B", "The office is closed today."),
                    ("C", "A report contains incorrect numbers."),
                    ("D", "The manager will arrive later than expected.")
                ),
                Item(
                    "p3-conversation-002",
                    3,
                    "Listening",
                    "A customer asks about a product return, and the employee explains the receipt policy.",
                    "Chọn mục đích chính của đoạn hội thoại.",
                    "A",
                    "Cuộc hội thoại xoay quanh việc hoàn trả sản phẩm.",
                    ("A", "To discuss returning an item."),
                    ("B", "To schedule a job interview."),
                    ("C", "To compare two office locations."),
                    ("D", "To announce a staff promotion.")
                ),
            ],
            [4] =
            [
                Item(
                    "p4-talk-001",
                    4,
                    "Listening",
                    "A station announcement says the 8:15 train is delayed due to maintenance work.",
                    "Bắt thông tin lý do chậm trễ.",
                    "B",
                    "Thông báo nêu maintenance work là lý do trễ tàu.",
                    ("A", "Severe weather."),
                    ("B", "Maintenance work."),
                    ("C", "A ticketing error."),
                    ("D", "A change in destination.")
                ),
                Item(
                    "p4-talk-002",
                    4,
                    "Listening",
                    "A voicemail asks employees to submit travel receipts before noon on Monday.",
                    "Chọn hạn chót được nhắc trong voicemail.",
                    "C",
                    "Deadline là trước trưa thứ Hai.",
                    ("A", "Friday morning."),
                    ("B", "Sunday evening."),
                    ("C", "Monday before noon."),
                    ("D", "Tuesday after lunch.")
                ),
            ],
            [5] =
            [
                Item(
                    "p5-word-form-001",
                    5,
                    "Reading",
                    "The marketing team needs a more ____ strategy for the new product.",
                    "Chọn dạng từ đúng theo vị trí trước danh từ.",
                    "B",
                    "Trước danh từ strategy cần tính từ effective.",
                    ("A", "effect"),
                    ("B", "effective"),
                    ("C", "effectively"),
                    ("D", "effectiveness")
                ),
                Item(
                    "p5-word-form-007",
                    5,
                    "Reading",
                    "The supervisor reviewed the report ____ before the meeting.",
                    "Chọn trạng từ bổ nghĩa cho động từ reviewed.",
                    "C",
                    "Blank bổ nghĩa cho reviewed nên cần adverb carefully.",
                    ("A", "careful"),
                    ("B", "care"),
                    ("C", "carefully"),
                    ("D", "carefulness")
                ),
            ],
            [6] =
            [
                Item(
                    "p6-text-001",
                    6,
                    "Reading",
                    "Thank you for registering for the workshop. ____ , please bring your laptop.",
                    "Chọn liên từ nối ý bổ sung trong đoạn văn.",
                    "A",
                    "Additionally dùng để thêm yêu cầu mới cùng mạch thông tin.",
                    ("A", "Additionally"),
                    ("B", "However"),
                    ("C", "Unless"),
                    ("D", "Despite")
                ),
                Item(
                    "p6-text-002",
                    6,
                    "Reading",
                    "The shipment was delayed. We apologize for any ____ this may have caused.",
                    "Chọn danh từ phù hợp trong cụm cố định.",
                    "D",
                    "Cụm thường dùng là any inconvenience this may have caused.",
                    ("A", "convenient"),
                    ("B", "conveniently"),
                    ("C", "inconvenient"),
                    ("D", "inconvenience")
                ),
            ],
            [7] =
            [
                Item(
                    "p7-reading-001",
                    7,
                    "Reading",
                    "Email: Please confirm whether you can attend the supplier meeting at 3 P.M. tomorrow.",
                    "Xác định mục đích chính của email.",
                    "B",
                    "Email yêu cầu người nhận xác nhận tham dự cuộc họp.",
                    ("A", "To cancel a supplier contract."),
                    ("B", "To ask for attendance confirmation."),
                    ("C", "To provide directions to a factory."),
                    ("D", "To summarize last year's sales.")
                ),
                Item(
                    "p7-reading-002",
                    7,
                    "Reading",
                    "Notice: The cafeteria will be closed Friday while new kitchen equipment is installed.",
                    "Chọn thông tin đúng theo notice.",
                    "A",
                    "Notice nói cafeteria đóng cửa thứ Sáu để lắp thiết bị mới.",
                    ("A", "The cafeteria will not open on Friday."),
                    ("B", "Employees must bring old equipment."),
                    ("C", "The kitchen will move to another building."),
                    ("D", "Lunch prices will increase next week.")
                ),
            ],
        };

    public IReadOnlyList<PartPracticeItemResponse> GetItems(int partId) =>
        itemsByPart.TryGetValue(partId, out var items) ? items : [];

    private static PartPracticeItemResponse Item(
        string itemId,
        int part,
        string skill,
        string prompt,
        string task,
        string correctAnswer,
        string explanation,
        params (string Key, string Value)[] options
    ) =>
        new(
            itemId,
            part,
            skill,
            prompt,
            task,
            options.ToDictionary(option => option.Key, option => option.Value),
            correctAnswer,
            explanation
        );
}

public sealed record PartPracticeItemResponse(
    string ItemId,
    int Part,
    string Skill,
    string Prompt,
    string Task,
    IReadOnlyDictionary<string, string> Options,
    string CorrectAnswer,
    string Explanation
);
