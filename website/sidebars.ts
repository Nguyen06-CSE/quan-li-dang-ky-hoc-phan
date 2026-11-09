import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    {
      type: 'doc',
      id: 'index',
      label: 'Tổng Quan Báo Cáo',
    },
    {
      type: 'category',
      label: 'Phần I: Đặc Tả Ứng Dụng',
      collapsed: false,
      items: [
        'dac-ta-ung-dung/tong-quan-yeu-cau',
        'dac-ta-ung-dung/kien-truc-he-thong',
        'dac-ta-ung-dung/co-so-du-lieu-erd',
        'dac-ta-ung-dung/ma-tran-phan-quyen',
        'dac-ta-ung-dung/design-system-ui-ux',
        'dac-ta-ung-dung/quy-chuan-lap-trinh',
        {
          type: 'category',
          label: 'Đặc Tả 11 Chức Năng',
          collapsed: false,
          items: [
            'dac-ta-ung-dung/chuc-nang/F01-xac-thuc-dang-nhap',
            'dac-ta-ung-dung/chuc-nang/F02-quan-ly-sinh-vien',
            'dac-ta-ung-dung/chuc-nang/F03-quan-ly-mon-hoc',
            'dac-ta-ung-dung/chuc-nang/F04-hoc-ky-lop-hoc-phan',
            'dac-ta-ung-dung/chuc-nang/F05-dang-ky-hoc-phan',
            'dac-ta-ung-dung/chuc-nang/F06-quan-ly-hoc-phi',
            'dac-ta-ung-dung/chuc-nang/F07-thoi-khoa-bieu',
            'dac-ta-ung-dung/chuc-nang/F08-bao-cao-thong-ke',
            'dac-ta-ung-dung/chuc-nang/F09-import-excel-du-lieu-lon',
            'dac-ta-ung-dung/chuc-nang/F10-hybrid-cache-delta-sync',
            'dac-ta-ung-dung/chuc-nang/F11-local-sqlite-git-backup',
            'dac-ta-ung-dung/chuc-nang/F11b-co-che-git-backup-chi-tiet',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'Phần II: Kết Quả Đạt Được',
      collapsed: false,
      items: [
        'ket-qua-dat-duoc/tong-ket-chuc-nang',
        'ket-qua-dat-duoc/kiem-thu-unit-test',
        'ket-qua-dat-duoc/toi-uu-hieu-nang',
        'ket-qua-dat-duoc/xuat-bao-cao-an-pham',
        'ket-qua-dat-duoc/huong-dan-cai-dat-khoi-chay',
      ],
    },
    {
      type: 'category',
      label: 'Phần III: Minh Chứng & Nhật Ký',
      collapsed: false,
      items: [
        'nhat-ky-va-minh-chung/lich-su-commit-github',
        'nhat-ky-va-minh-chung/nhat-ky-cong-viec-daily',
        'nhat-ky-va-minh-chung/nhat-ky-quyet-dinh-ky-thuat',
        'nhat-ky-va-minh-chung/nhat-ky-su-co-va-sua-loi',
        'nhat-ky-va-minh-chung/nhat-ky-prompts-agent',
      ],
    },
  ],
};

export default sidebars;
